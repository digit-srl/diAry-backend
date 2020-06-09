using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiaryCollector.OutputModels {
    
    public class CallToActionMatch {

        public bool HasMatch { get; set; }

        public CallToAction[] Calls { get; set; }

        public class CallToAction {

            public string Id { get; set; }

            public string Description { get; set; }

            public string Url { get; set; }

            public string Source { get; set; }

            public string SourceName { get; set; }

            public string SourceDescription { get; set; }

            public int ExposureSeconds { get; set; }

            public CallToActionQuery[] Queries { get; set; }

        }

        public class CallToActionQuery {

            public DateTime From { get; set; }

            public DateTime To { get; set; }

            public DateTime LastUpdate { get; set; }

            public GeoJsonGeometry Geometry { get; set; }

        }

        public class GeoJsonGeometry {

            public string Type { get; set; }

            public double[][][] Coordinates { get; set; }

        }

    }

}
