using IvanConnections_Travel.Models.Enums;
using System.Text.Json.Serialization;

namespace IvanConnections_Travel.Models
{
    public class StopArrival
    {
        // [JsonPropertyName("vehicleId")]
        // public string VehicleId { get; set; }
        
        [JsonPropertyName("vehicleLabel")]
        public string VehicleLabel { get; set; }

        [JsonPropertyName("arrivalMinutes")]
        public double ArrivalMinutes { get; set; }

        [JsonPropertyName("vehicleType")]
        public VehicleType VehicleType { get; set; }

    }
}