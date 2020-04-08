using DiaryCollector.InputModels;
using DiaryCollector.OutputModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Driver.GeoJsonObjectModel;
using System;
using System.Globalization;
using System.Threading.Tasks;
using WomPlatform.Connector.Models;

namespace DiaryCollector.Controllers {

    [Route("api")]
    public class ApiController : ControllerBase {

        private readonly MongoConnector Mongo;
        private readonly WomService Wom;
        private readonly ILogger<ApiController> Logger;
        private readonly Geohash.Geohasher Geohasher = new Geohash.Geohasher();
        private const int MinutesADay = 24 * 60;

        private static readonly DateTime MinDate = new DateTime(2020, 4, 2);

        public ApiController(
            MongoConnector mongo,
            WomService wom,
            ILogger<ApiController> logger
        ) {
            Mongo = mongo;
            Wom = wom;
            Logger = logger;
        }

        [HttpPost("upload")]
        [ServiceFilter(typeof(RequireApiKeyAttribute))]
        public async Task<IActionResult> Upload(
            [FromBody] DailyStats stats
        ) {
            if(!ModelState.IsValid) {
                Logger.LogError("Failed to parse input data: {0}", ModelState);
                return BadRequest();
            }

            Logger.LogInformation("Receiving daily stats from device {0} for {1}", stats.InstallationId, stats.Date.ToString("d", CultureInfo.InvariantCulture));

            // Safety checks
            if(stats.Date < MinDate) {
                Logger.LogError("Daily statistics for unacceptable date {0}", stats.Date);
                return UnprocessableEntity();
            }
            if(stats.TotalMinutesTracked > MinutesADay) {
                Logger.LogError("Total minutes tracked ({0}) exceeds minutes in a day", stats.TotalMinutesTracked);
                return UnprocessableEntity();
            }
            if(stats.Date >= DateTime.UtcNow.Date) {
                Logger.LogError("Daily statistics for non-elapsed day {0}", stats.Date.Date);
                return UnprocessableEntity();
            }
            
            GeoJsonPoint<GeoJson2DGeographicCoordinates> position;
            try {
                var decoded = Geohasher.Decode(stats.CentroidHash);
                position = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new GeoJson2DGeographicCoordinates(decoded.Item2, decoded.Item1));
            }
            catch(Exception ex) {
                Logger.LogError(ex, "Failed to decode Geohash '{0}'", stats.CentroidHash);
                return UnprocessableEntity();
            }

            if(stats.LocationTracking == null) {
                Logger.LogError("Payload does not contain location tracking section");
                return UnprocessableEntity();
            }
            if(stats.LocationTracking.MinutesAtHome < 0 ||
               stats.LocationTracking.MinutesAtWork < 0 ||
               stats.LocationTracking.MinutesAtSchool < 0 ||
               stats.LocationTracking.MinutesAtOtherKnownLocations < 0 ||
               stats.LocationTracking.MinutesElsewhere < 0) {
                Logger.LogError("Location tracking minutes cannot be negative");
                return UnprocessableEntity();
            }
            if(stats.LocationTracking.MinutesAtHome +
               stats.LocationTracking.MinutesAtWork +
               stats.LocationTracking.MinutesAtSchool +
               stats.LocationTracking.MinutesAtOtherKnownLocations +
               stats.LocationTracking.MinutesElsewhere
               > MinutesADay) {
                Logger.LogError("Location tracking section exceeds minutes in a day");
                return UnprocessableEntity();
            }

            // Check for duplicates
            var existingStats = await Mongo.GetDailyStats(stats.InstallationId, stats.Date);
            if(existingStats != null) {
                Logger.LogError("Duplicate statistics from device ID {0} for date {1}", stats.InstallationId, stats.Date.ToString("d", CultureInfo.InvariantCulture));
                return Conflict();
            }

            // Compute voucher amounts
            int womCount = (int)(Math.Floor(stats.TotalMinutesTracked / 60.0) + Math.Floor(stats.LocationTracking.MinutesAtHome / 60.0));
            Logger.LogInformation("Generating {0} WOM vouchers for {1} total minutes and {2} minutes at home", womCount, stats.TotalMinutesTracked, stats.LocationTracking.MinutesAtHome);
            var voucherRequest = await Wom.Instrument.RequestVouchers(new VoucherCreatePayload.VoucherInfo[] {
                new VoucherCreatePayload.VoucherInfo {
                    Aim = "HE",
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

    }

}
