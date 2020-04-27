using System;

namespace DiaryCollector.InputModels {
    
    public class DataCheck {

        public DateTime LastCheckTimestamp { get; set; }

        public ActivitySlice[] Activities { get; set; }

    }

}
