namespace IvanConnections_Travel.Models
{
    public class Stop
    {
        public int StopId { get; set; }

        public string StopName { get; set; } = null!;

        public string? StopDesc { get; set; }

        public double StopLat { get; set; }

        public double StopLon { get; set; }

        public int LocationType { get; set; }

        public string? StopCode { get; set; }
        public int? OppositeStopId { get; set; }
    }
}
