using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace DiaryCollector.DataModels {

    public class LocationTrackingStats {

        [BsonElement("minutesAtHome")]
        public int MinutesAtHome { get; set; }

        [BsonElement("minutesAtWork")]
        public int MinutesAtWork { get; set; }

        [BsonElement("minutesAtSchool")]
        public int MinutesAtSchool { get; set; }

        [BsonElement("minutesAtOtherKnownLocations")]
        public int MinutesAtOtherKnownLocations { get; set; }

        [BsonElement("minutesElsewhere")]
        public int MinutesElsewhere { get; set; }

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }

    }

}
