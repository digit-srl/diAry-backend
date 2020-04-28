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

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }

    }

}
