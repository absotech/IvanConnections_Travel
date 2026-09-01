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
        public int? RouteId { get; set; }
        public string? TripHeadsign { get; set; }
        public string? RouteColor { get; set; }
        public string? PreviousStopName { get; set; }
        public Stop? NextStop { get; set; }
        public DateTime? TimeOfArrival { get; set; }
        public bool IsElectricBus { get; set; }

        public bool IsNewTram { get; set; }
        public bool IsTracked { get; set; } = false;

        /// <summary>
        /// Numărul de minute după care datele de poziție ale unui vehicul sunt considerate învechite.
        /// </summary>
        public const int StaleThresholdMinutes = 2;

        /// <summary>
        /// Momentul (UTC) al ultimei actualizări a poziției vehiculului.
        /// </summary>
        public DateTime? LastUpdateUtc => LocalTimestamp ?? Timestamp;

        /// <summary>
        /// True dacă poziția vehiculului nu a mai fost actualizată de mai mult de
        /// <see cref="StaleThresholdMinutes"/> minute, deci este posibil ca vehiculul
        /// să nu se mai afle în acel loc.
        /// </summary>
        public bool IsDataStale
        {
            get
            {
                if (!LastUpdateUtc.HasValue) return false;
                return DateTime.UtcNow - LastUpdateUtc.Value > TimeSpan.FromMinutes(StaleThresholdMinutes);
            }
        }
    }
}
