using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace DiaryCollector.DataModels {

    public class MovementTrackingStats {

        [BsonElement("static")]
        public int Static { get; set; }

        [BsonElement("vehicle")]
        public int Vehicle { get; set; }

        [BsonElement("bicycle")]
        public int Bicycle { get; set; }

        [BsonElement("onFoot")]
        public int OnFoot { get; set; }

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }

    }

}
