using DiaryCollector.InputModels;
using DiaryCollector.OutputModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver.GeoJsonObjectModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using WomPlatform.Connector.Models;

namespace DiaryCollector.Controllers {

    [Route("api")]
    public class ApiController : BaseController {

        private readonly IOptions<ApiConfiguration> ApiConf;
        private readonly MongoConnector Mongo;
        private readonly WomService Wom;
        private const int MinutesADay = 24 * 60;

        private static readonly DateTime MinDate = new DateTime(2020, 4, 2);

        public ApiController(
            IOptions<ApiConfiguration> apiConf,
            MongoConnector mongo,
            WomService wom,
            LinkGenerator linkGenerator,
            ILogger<ApiController> logger
        ) : base(linkGenerator, logger)
        {
            ApiConf = apiConf;
            Mongo = mongo;
            Wom = wom;
        }

        [HttpPost("upload")]
        [ServiceFilter(typeof(RequireApiKeyAttribute))]
        public async Task<IActionResult> Upload(
            [FromBody] DailyStats stats
        ) {
            if (!ModelState.IsValid) {
                Logger.LogError("Failed to parse input data: {0}", ModelState);
                return BadRequest(ModelState);
            }

            Logger.LogInformation("Receiving daily stats from device {0} for {1}", stats.InstallationId, stats.Date.ToString("d", CultureInfo.InvariantCulture));

            // Safety checks
            if (stats.Date < MinDate) {
                Logger.LogError("Daily statistics for unacceptable date {0}", stats.Date);
                return UnprocessableEntity(ProblemDetailsFactory.CreateProblemDetails(HttpContext,
                    title: "Unacceptable date (out of valid range)",
                    type: "https://arianna.digit.srl/api/problems/invalid-date"
                ));
            }
            if (stats.TotalMinutesTracked > MinutesADay) {
                Logger.LogError("Total minutes tracked ({0}) exceeds minutes in a day", stats.TotalMinutesTracked);
                return UnprocessableEntity(ProblemDetailsFactory.CreateProblemDetails(HttpContext,
                    title: "Total minutes tracked exceeds minutes in a day",
                    type: "https://arianna.digit.srl/api/problems/invalid-data"
                ));
            }
            if (stats.Date >= DateTime.UtcNow.Date) {
                Logger.LogError("Daily statistics for non-elapsed day {0}", stats.Date.Date);
                return UnprocessableEntity(ProblemDetailsFactory.CreateProblemDetails(HttpContext,
                    title: "Unacceptable date (future date)",
                    type: "https://arianna.digit.srl/api/problems/invalid-date"
                ));
            }

            GeoJsonPoint<GeoJson2DGeographicCoordinates> position;
            string geohash;
            try {
                geohash = stats.CentroidHash.Substring(0, 5);
                var decoded = Geohasher.Decode(geohash);
                position = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new GeoJson2DGeographicCoordinates(decoded.Item2, decoded.Item1));
                Logger.LogInformation("GeoHash {0} decoded as {1:F5},{2:F5}", geohash, position.Coordinates.Latitude, position.Coordinates.Longitude);
            }
            catch (Exception ex) {
                return UnprocessableEntity(ProblemDetailsFactory.CreateProblemDetails(HttpContext,
                    title: "Cannot decode geohash",
                    type: "https://arianna.digit.srl/api/problems/invalid-data",
                    detail: ex.Message
                ));
            }

            if (stats.LocationTracking == null) {
                Logger.LogError("Payload does not contain location tracking section");
                return UnprocessableEntity(ProblemDetailsFactory.CreateProblemDetails(HttpContext,
                    title: "Payload does not contain location tracking section",
                    type: "https://arianna.digit.srl/api/problems/invalid-data"
                ));
            }
            if (stats.LocationTracking.MinutesAtHome < 0 ||
               stats.LocationTracking.MinutesAtWork < 0 ||
               stats.LocationTracking.MinutesAtSchool < 0 ||
               stats.LocationTracking.MinutesAtOtherKnownLocations < 0 ||
               stats.LocationTracking.MinutesElsewhere < 0) {
                Logger.LogError("Location tracking minutes cannot be negative");
                return UnprocessableEntity(ProblemDetailsFactory.CreateProblemDetails(HttpContext,
                    title: "Negative location tracking value",
                    type: "https://arianna.digit.srl/api/problems/invalid-data"
                ));
            }
            if (stats.LocationTracking.MinutesAtHome > MinutesADay ||
               stats.LocationTracking.MinutesAtWork > MinutesADay ||
               stats.LocationTracking.MinutesAtSchool > MinutesADay ||
               stats.LocationTracking.MinutesAtOtherKnownLocations > MinutesADay ||
               stats.LocationTracking.MinutesElsewhere > MinutesADay) {
                Logger.LogError("One entry in the location tracking section exceeds minutes in a day");
                return UnprocessableEntity(ProblemDetailsFactory.CreateProblemDetails(HttpContext,
                    title: "One entry in the location tracking section exceeds minutes in a day",
                    type: "https://arianna.digit.srl/api/problems/invalid-data"
                ));
            }

            // Check for duplicates
            var existingStats = await Mongo.GetDailyStats(stats.InstallationId, stats.Date);
            if (existingStats != null) {
                Logger.LogError("Duplicate statistics from device ID {0} for date {1}", stats.InstallationId, stats.Date.ToString("d", CultureInfo.InvariantCulture));
                return Conflict(ProblemDetailsFactory.CreateProblemDetails(HttpContext,
                    title: "Duplicate statistics for date",
                    type: "https://arianna.digit.srl/api/problems/duplicate"
                ));
            }

            // Compute voucher amounts
            int stayAtHomeBonus = 0;
            int womCount = (int)Math.Ceiling(stats.TotalMinutesTracked / 60.0) + stayAtHomeBonus;
            Logger.LogInformation("Generating {0} WOM vouchers for {1} total minutes and {2} minutes at home ({3} stay at home bonus)",
                womCount, stats.TotalMinutesTracked, stats.LocationTracking.MinutesAtHome, stayAtHomeBonus);
            var voucherRequest = await Wom.Instrument.RequestVouchers(new VoucherCreatePayload.VoucherInfo[] {
                new VoucherCreatePayload.VoucherInfo {
                    Aim = "P",
                    Count = womCount,
                    Latitude = position.Coordinates.Latitude,
                    Longitude = position.Coordinates.Longitude,
                    Timestamp = stats.Date.Date.AddHours(23.999)
                }
            });

            // OK-dokey
            await Mongo.AddDailyStats(new DataModels.DailyStats {
                InstallationId = stats.InstallationId,
                Date = stats.Date.Date,
                TotalMinutesTracked = stats.TotalMinutesTracked,
                TotalWomVouchersEarned = womCount,
                Centroid = position,
                CentroidHash = geohash,
                LocationCount = stats.LocationCount,
                VehicleCount = stats.VehicleCount,
                EventCount = stats.EventCount,
                SampleCount = stats.SampleCount,
                DiscardedSampleCount = stats.DiscardedSampleCount,
                BoundingBoxDiagonal = stats.BoundingBoxDiagonal,
                LocationTracking = new DataModels.LocationTrackingStats {
                    MinutesAtHome = stats.LocationTracking.MinutesAtHome,
                    MinutesAtWork = stats.LocationTracking.MinutesAtWork,
                    MinutesAtSchool = stats.LocationTracking.MinutesAtSchool,
                    MinutesAtOtherKnownLocations = stats.LocationTracking.MinutesAtOtherKnownLocations,
                    MinutesElsewhere = stats.LocationTracking.MinutesElsewhere
                }
            });

            return Ok(new UploadConfirmation {
                WomLink = voucherRequest.Link,
                WomPassword = voucherRequest.Password,
                WomCount = womCount
            });
        }

        [HttpPost("check")]
        [ServiceFilter(typeof(RequireApiKeyAttribute))]
        public async Task<IActionResult> Upload(
            [FromBody] DataCheck data
        ) {
            if(data == null || data.Activities == null) {
                return BadRequest();
            }
            Logger.LogInformation("Received query on {0} activity slices, last check {1}",
                data.Activities.Length, data.LastCheckTimestamp);

            if(!ApiConf.Value.PerformTimestampFiltering) {
                Logger.LogDebug("Timestamp filtering disabled");
            }
            if(ApiConf.Value.PerformTimestampFiltering && !data.LastCheckTimestamp.HasValue) {
                Logger.LogDebug("No last check, checking from min value");
                data.LastCheckTimestamp = DateTime.MinValue;
            }

            var ctas = new Dictionary<ObjectId, DataModels.CallToAction>();
            foreach(var activity in data.Activities) {
                var matchCtas = await Mongo.MatchFilter(activity.Date, activity.Hashes,
                    ApiConf.Value.PerformTimestampFiltering ? data.LastCheckTimestamp.Value : DateTime.MinValue);
                
                Logger.LogInformation("Geohashes {0} on {1} matches {2} calls ({3})",
                    string.Join(", ", activity.Hashes),
                    activity.Date.ToShortDateString(),
                    matchCtas.Count,
                    string.Join(", ", from id in matchCtas select id.Id.ToString()));
                
                ctas.AddRange(matchCtas, cta => cta.Id);
            }

            Logger.LogInformation("Loading filters for {0} unique call to actions", ctas.Count);
            var filters = await Mongo.GetCallToActionFilters(ctas.Keys);
            var filterMap = filters.GroupBy(f => f.CallToActionId)
                .ToDictionary(g => g.Key, g => g.Select(cta => cta));

            Logger.LogInformation("Found {0} call to actions ({1}) with {2} filters ({3})",
                ctas.Count, string.Join(", ", from ctaId in ctas.Keys select ctaId.ToString()),
                filters.Count, string.Join(", ", from filter in filters select filter.Id.ToString())
            );

            return Ok(new CallToActionMatch {
                HasMatch = ctas.Count > 0,
                Calls = (from cta in ctas.Values
                         select new CallToActionMatch.CallToAction {
                             Id = cta.Id.ToString(),
                             Description = cta.Description,
                             Url = string.IsNullOrEmpty(cta.Url)
                                 ? GenerateActionLink(nameof(CallToActionController.Show), "CallToAction", new { id = cta.Id.ToString() })
                                 : cta.Url,
                             Source = cta.SourceKey,
                             SourceName = cta.SourceName,
                             SourceDescription = cta.SourceDescription,
                             ExposureSeconds = cta.ExposureSeconds,
                             Queries = (from filter in filterMap[cta.Id]
                                        select new CallToActionMatch.CallToActionQuery {
                                            From = filter.TimeBegin,
                                            To = filter.TimeEnd,
                                            LastUpdate = filter.AddedOn,
                                            Geometry = new CallToActionMatch.GeoJsonGeometry {
                                                Type = "Polygon",
                                                Coordinates = filter.Geometry.ToPolygonArray()
                                            }
                                        }).ToArray()
                         }).ToArray()
            });
        }

    }

}
