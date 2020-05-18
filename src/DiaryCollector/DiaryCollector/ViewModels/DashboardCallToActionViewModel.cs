using DiaryCollector.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiaryCollector.ViewModels {
    
    public class DashboardCallToActionViewModel {

        public CallToAction Call { get; set; }

        public List<CallToActionFilter> Filters { get; set; }

    }

}
