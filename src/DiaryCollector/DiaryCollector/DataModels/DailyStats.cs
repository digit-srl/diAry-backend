﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver.GeoJsonObjectModel;
using System;

namespace DiaryCollector.DataModels {
    
    public class DailyStats {

        [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
        public ObjectId Id { get; set; }

        [BsonElement("deviceId")]
        public Guid DeviceId { get; set; }

        [BsonElement("date")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Date { get; set; }

        [BsonElement("totalMinutesTracked")]
        public int TotalMinutesTracked { get; set; }

        [BsonElement("totalWomVouchersEarned")]
        public int TotalWomVouchersEarned { get; set; }

        [BsonElement("centroid")]
        public GeoJsonPoint<GeoJson2DGeographicCoordinates> Centroid { get; set; }

        [BsonElement("locationCount")]
        public int LocationCount { get; set; }

        [BsonElement("vehicleCount")]
        public int VehicleCount { get; set; }

        [BsonElement("eventCount")]
        public int EventCount { get; set; }

        [BsonElement("locationTracking")]
        public LocationTrackingStats LocationTracking { get; set; }

        [BsonElement("movementTracking")]
        public MovementTrackingStats MovementTracking { get; set; }

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }

    }

}
