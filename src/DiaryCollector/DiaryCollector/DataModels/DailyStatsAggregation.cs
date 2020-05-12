using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiaryCollector.DataModels {
    
    public class DailyStatsAggregation {

        [BsonElement("_id")]
        public DateTime Day { get; set; }

        [BsonElement("count")]
        public int Count { get; set; }

        [BsonElement("avgMinutesTracked")]
        public double AverageMinutesTracked { get; set; }

        [BsonElement("totalMinutesTracked")]
        public double TotalMinutesTracked { get; set; }

        [BsonElement("avgMinutesAtHome")]
        public double AverageMinutesAtHome { get; set; }

        [BsonElement("totalMinutesAtHome")]
        public double TotalMinutesAtHome { get; set; }

        [BsonElement("avgBoundingBoxDiagonal")]
        public double AverageBoundingBoxDiagonal { get; set; }

    }

}
