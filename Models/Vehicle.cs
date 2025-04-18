using IvanConnections_Travel.Models.Enums;
using System;
namespace IvanConnections_Travel.Models
{
    public class Vehicle
    {
        public int Id { get; set; }
        public string? Label { get; set; }
        public double? Latitude { get; set; }

        public double? Longitude { get; set; }
        public DateTime? Timestamp { get; set; }

        public VehicleType? VehicleType { get; set; }

        public string? BikeAccessible { get; set; }

        public string? WheelchairAccessible { get; set; }

        public int? Speed { get; set; }
        public double? Direction { get; set; }
        public DateTime? LocalTimestamp { get; set; }
        public string? RouteShortName { get; set; }
        public string? TripHeadsign { get; set; }
        public string? RouteColor { get; set; }
        public string? PreviousStopName { get; set; }
        public Stop? NextStop { get; set; }
        public DateTime? TimeOfArrival { get; set; }
        public bool IsElectricBus { get; set; }

        public bool IsNewTram { get; set; }
    }
}
