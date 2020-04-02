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
            Logger.LogInformation("Receiving daily stats from device {0} for {1}", stats.DeviceId, stats.Date.ToString("d", CultureInfo.InvariantCulture));

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
            if(stats.LocationTracking.MinutesAtHome +
               stats.LocationTracking.MinutesAtWork +
               stats.LocationTracking.MinutesAtSchool +
               stats.LocationTracking.MinutesAtOtherKnownLocations +
               stats.LocationTracking.MinutesElsewhere
               > MinutesADay) {
                Logger.LogError("Location tracking section exceeds minutes in a day");
                return UnprocessableEntity();
            }

            if(stats.MovementTracking == null) {
                Logger.LogError("Payload does not contain movement tracking section");
                return UnprocessableEntity();
            }
            if(stats.MovementTracking.Static +
               stats.MovementTracking.Vehicle +
               stats.MovementTracking.Bicycle +
               stats.MovementTracking.OnFoot
               > MinutesADay) {
                Logger.LogError("Movement tracking section exceeds minutes in a day");
                return UnprocessableEntity();
            }

            // Check for duplicates
            var existingStats = await Mongo.GetDailyStats(stats.DeviceId, stats.Date);
            if(existingStats != null) {
                Logger.LogError("Duplicate statistics from device ID {0} for date {1}", stats.DeviceId, stats.Date.ToString("d", CultureInfo.InvariantCulture));
                return Conflict();
            }

            // Compute voucher amounts
            int womCount = (int)(Math.Floor(stats.TotalMinutesTracked / 60.0) + Math.Floor(stats.LocationTracking.MinutesAtHome / 60.0));
            Logger.LogInformation("Generating {0} WOM vouchers for {1} total minutes and {2} minutes at home", womCount, stats.TotalMinutesTracked, stats.LocationTracking.MinutesAtHome);
            (var womOtc, var womPwd) = await Wom.Instrument.RequestVouchers(new VoucherCreatePayload.VoucherInfo[] {
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
                DeviceId = stats.DeviceId,
                Date = stats.Date.Date,
                TotalMinutesTracked = stats.TotalMinutesTracked,
                TotalWomVouchersEarned = womCount,
                Centroid = position,
                LocationCount = stats.LocationCount,
                VehicleCount = stats.VehicleCount,
                EventCount = stats.EventCount,
                LocationTracking = new DataModels.LocationTrackingStats {
                    MinutesAtHome = stats.LocationTracking.MinutesAtHome,
                    MinutesAtWork = stats.LocationTracking.MinutesAtWork,
                    MinutesAtSchool = stats.LocationTracking.MinutesAtSchool,
                    MinutesAtOtherKnownLocations = stats.LocationTracking.MinutesAtOtherKnownLocations,
                    MinutesElsewhere = stats.LocationTracking.MinutesElsewhere
                },
                MovementTracking = new DataModels.MovementTrackingStats {
                    Static = stats.MovementTracking.Static,
                    Vehicle = stats.MovementTracking.Vehicle,
                    Bicycle = stats.MovementTracking.Bicycle,
                    OnFoot = stats.MovementTracking.OnFoot
                }
            });

            return Ok(new UploadConfirmation {
                WomLink = $"https://{Wom.Domain}/vouchers/{womOtc:N}",
                WomPassword = womPwd
            });
        }

    }

}
