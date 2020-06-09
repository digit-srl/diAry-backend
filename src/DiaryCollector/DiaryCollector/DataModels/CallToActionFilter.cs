using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver.GeoJsonObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiaryCollector.DataModels {
    public class CallToActionFilter {

        [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
        public ObjectId Id { get; set; }

        [BsonElement("callToActionId")]
        public ObjectId CallToActionId { get; set; }

        [BsonElement("addedOn")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime AddedOn { get; set; }

        [BsonElement("timeBegin")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime TimeBegin { get; set; }

        [BsonElement("timeEnd")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime TimeEnd { get; set; }

        [BsonElement("geometry")]
        public GeoJsonGeometry<GeoJson2DGeographicCoordinates> Geometry { get; set; }

        [BsonElement("coveringGeohash")]
        public string[] CoveringGeohash { get; set; }

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }

    }
}
