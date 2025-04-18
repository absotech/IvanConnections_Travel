using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
