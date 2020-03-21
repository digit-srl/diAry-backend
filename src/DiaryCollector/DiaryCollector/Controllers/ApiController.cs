using DiaryCollector.InputModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Driver.GeoJsonObjectModel;
using System;
using System.Threading.Tasks;

namespace DiaryCollector.Controllers {
    
    [Route("api")]
    public class ApiController : ControllerBase {

        private readonly MongoConnector Mongo;
        private readonly ILogger<ApiController> Logger;
        private readonly Geohash.Geohasher Geohasher = new Geohash.Geohasher();
        private const int MinutesADay = 24 * 60;

        private static readonly DateTime MinDate = new DateTime(2020, 3, 22);

        public ApiController(
            MongoConnector mongo,
            ILogger<ApiController> logger
        ) {
            Mongo = mongo;
            Logger = logger;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(
            [FromBody] DailyStats stats
        ) {
            Logger.LogInformation("Receiving daily stats from device {0}", stats.DeviceId);

            // Safety checks
            if(stats.Date < MinDate) {
                Logger.LogError("Daily statistics for unacceptable date {0}", stats.Date);
                return UnprocessableEntity();
            }
            if(stats.TotalMinutesTracked > MinutesADay) {
                Logger.LogError("Total minutes tracked ({0}) exceeds minutes in a day", stats.TotalMinutesTracked);
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

            // OK-dokey
            await Mongo.AddDailyStats(new DataModels.DailyStats {
                Date = stats.Date.Date,
                TotalMinutesTracked = stats.TotalMinutesTracked,
                TotalWomVouchersEarned = 0,
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

            return Ok();
        }

    }

}
