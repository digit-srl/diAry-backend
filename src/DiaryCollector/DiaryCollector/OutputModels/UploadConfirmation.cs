using System;

namespace DiaryCollector.OutputModels {
    
    public class UploadConfirmation {

        public string Status { get; set; } = "success";

        public string WomLink { get; set; }

        public string WomPassword { get; set; }

    }

}
