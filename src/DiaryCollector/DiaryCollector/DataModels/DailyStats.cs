using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver.GeoJsonObjectModel;
using System;

namespace DiaryCollector.DataModels {
    
    public class DailyStats {

        [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
        public ObjectId Id { get; set; }

        [BsonElement("installationId")]
        public Guid InstallationId { get; set; }

        [BsonElement("date")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime Date { get; set; }

        [BsonElement("totalMinutesTracked")]
        public int TotalMinutesTracked { get; set; }

        [BsonElement("totalWomVouchersEarned")]
        public int TotalWomVouchersEarned { get; set; }

        [BsonElement("centroid")]
        public GeoJsonPoint<GeoJson2DGeographicCoordinates> Centroid { get; set; }

        [BsonElement("centroidHash")]
        [BsonIgnoreIfNull]
        public string CentroidHash { get; set; }

        [BsonElement("locationCount")]
        public int LocationCount { get; set; }

        [BsonElement("vehicleCount")]
        public int VehicleCount { get; set; }

        [BsonElement("eventCount")]
        public int EventCount { get; set; }

        [BsonElement("sampleCount")]
        [BsonIgnoreIfDefault]
        public int SampleCount { get; set; }

        [BsonElement("discardedSampleCount")]
        [BsonIgnoreIfDefault]
        public int DiscardedSampleCount { get; set; }

        [BsonElement("boundingBoxDiagonal")]
        [BsonIgnoreIfDefault]
        public double BoundingBoxDiagonal { get; set; }

        [BsonElement("locationTracking")]
        public LocationTrackingStats LocationTracking { get; set; }

        [BsonElement("movementTracking")]
        [BsonIgnoreIfNull]
        public MovementTrackingStats MovementTracking { get; set; }

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }

    }

}
