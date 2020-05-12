﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace DiaryCollector.Controllers {

    [Route("data")]
    public class DataAccessController : BaseController {

        private readonly MongoConnector Mongo;

        public DataAccessController(
            MongoConnector mongo,
            LinkGenerator linker,
            ILogger<DataAccessController> logger
         ) : base(linker, logger) {
            Mongo = mongo;
        }

        [HttpGet("dump")]
        public async Task FullDump() {
            var lastAccess = await Mongo.GetLastDailyStats();
            Response.Headers[HeaderNames.LastModified] = lastAccess.Date.ToUniversalTime().ToString("R");
            Response.Headers[HeaderNames.ContentType] = "text/csv";
            Response.StatusCode = (int)HttpStatusCode.OK;

            await Response.WriteAsync("InstallationId,TotalMinutesTracked,CentroidGeohash,CentroidLat,CentroidLong,LocationCount,VehicleCount,EventCount,SampleCount,DiscardedSampleCount,BoundingBoxDiagonal,MinAtHome,MinAtWork,MinAtSchool,MinAtLocations,MinElsewhere" + Environment.NewLine);

            var cursor = await Mongo.FetchAllDailyStats();
            while(await cursor.MoveNextAsync()) {
                foreach (var stat in cursor.Current) {
                    await Response.WriteAsync(string.Join(",",
                        stat.InstallationId.ToString("N"),
                        stat.TotalMinutesTracked,
                        Geohasher.Encode(stat.Centroid.Coordinates.Latitude, stat.Centroid.Coordinates.Longitude, 5),
                        stat.Centroid.Coordinates.Latitude.ToString("F5"),
                        stat.Centroid.Coordinates.Longitude.ToString("F5"),
                        stat.LocationCount,
                        stat.VehicleCount,
                        stat.EventCount,
                        stat.SampleCount,
                        stat.DiscardedSampleCount,
                        stat.BoundingBoxDiagonal.ToString("F2"),
                        stat.LocationTracking.MinutesAtHome,
                        stat.LocationTracking.MinutesAtWork,
                        stat.LocationTracking.MinutesAtSchool,
                        stat.LocationTracking.MinutesAtOtherKnownLocations,
                        stat.LocationTracking.MinutesElsewhere
                    ) + Environment.NewLine);
                }
            }
        }

        [HttpGet("stats")]
        public async Task StatsAggregation() {
            Response.Headers[HeaderNames.ContentType] = "text/csv";
            Response.StatusCode = (int)HttpStatusCode.OK;

            await Response.WriteAsync("Day,StatsCount,AvgMinutesTracked,TotalMinutesTracked,AvgMinutesAtHome,TotalMinutesAtHome,AvgBoundingBoxDiagonal" + Environment.NewLine);

            var cursor = await Mongo.GetAggregatedDailyStats();
            while (await cursor.MoveNextAsync()) {
                foreach (var stats in cursor.Current) {
                    await Response.WriteAsync(string.Join(",",
                        stats.Day.ToString("yyyy-MM-dd"),
                        stats.Count,
                        stats.AverageMinutesTracked.ToString("F2"),
                        stats.TotalMinutesTracked,
                        stats.AverageMinutesAtHome.ToString("F2"),
                        stats.TotalMinutesAtHome,
                        stats.AverageBoundingBoxDiagonal.ToString("F2")
                    ) + Environment.NewLine);
                }
            }
        }

    }

}
