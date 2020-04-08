using System;

namespace DiaryCollector.InputModels {
    
    public class DailyStats {

        public Guid InstallationId { get; set; }

        public DateTime Date { get; set; }

        public int TotalMinutesTracked { get; set; }

        public int TotalWomVouchersEarned { get; set; }

        public string CentroidHash { get; set; }

        public int LocationCount { get; set; }

        public int VehicleCount { get; set; }

        public int EventCount { get; set; }

        public int SampleCount { get; set; }

        public int DiscardedSampleCount { get; set; }

        public double BoundingBoxDiagonal { get; set; }

        public class LocationTrackingStats {

            public int MinutesAtHome { get; set; }

            public int MinutesAtWork { get; set; }

            public int MinutesAtSchool { get; set; }

            public int MinutesAtOtherKnownLocations { get; set; }

            public int MinutesElsewhere { get; set; }

        }

        public LocationTrackingStats LocationTracking { get; set; }

        public class MovementTrackingStats {

            public int Static { get; set; }

            public int Vehicle { get; set; }

            public int Bicycle { get; set; }

            public int OnFoot { get; set; }

        }

    }

}
