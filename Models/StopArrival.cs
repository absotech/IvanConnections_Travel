using IvanConnections_Travel.Models.Enums;
using System.Text.Json.Serialization;

namespace IvanConnections_Travel.Models
{
    public class StopArrival
    {
        [JsonPropertyName("vehicleLabel")]
        public string VehicleLabel { get; set; }

        [JsonPropertyName("tripId")]
        public string TripId { get; set; }

        [JsonPropertyName("routeId")]
        public int RouteId { get; set; }

        [JsonPropertyName("arrivalMinutes")]
        public double ArrivalMinutes { get; set; }

        [JsonPropertyName("vehicleType")]
        public VehicleType VehicleType { get; set; }

        public int ArrivalTimeInMinutes => (int)Math.Ceiling(ArrivalMinutes);
    }
}