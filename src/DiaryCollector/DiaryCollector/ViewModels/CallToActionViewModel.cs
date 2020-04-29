using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiaryCollector.ViewModels {
    
    public class CallToActionViewModel {

        public string Id { get; set; }

        public DateTime From { get; set; }

        public DateTime To { get; set; }

        public double[][] PolygonCoordinates { get; set; }

        public double[] BoundingBox { get; set; }

        public string Description { get; set; }

    }

}
