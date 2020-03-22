using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using System;

namespace DiaryCollector.DataModels {

    public class ApiKey {

        [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
        public ObjectId Id { get; set; }

        [BsonElement("key")]
        public string Key { get; set; }

        [BsonElement("userAgent")]
        public string UserAgent { get; set; }

        [BsonElement("registeredOn")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime RegisteredOn { get; set; }

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }

    }

}
