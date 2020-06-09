using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using System;

namespace DiaryCollector.DataModels {
    
    public class CallToAction {

        [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
        public ObjectId Id { get; set; }

        [BsonElement("description")]
        public string Description { get; set; }

        [BsonElement("url")]
        public string Url { get; set; }

        [BsonElement("source")]
        [BsonIgnoreIfNull]
        public string SourceKey { get; set; }

        [BsonElement("sourceName")]
        [BsonIgnoreIfNull]
        public string SourceName { get; set; }

        [BsonElement("sourceDescription")]
        [BsonIgnoreIfNull]
        public string SourceDescription { get; set; }

        [BsonElement("exposureSeconds")]
        [BsonDefaultValue(0)]
        [BsonIgnoreIfDefault]
        public int ExposureSeconds { get; set; } = 0;

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }

    }

}
