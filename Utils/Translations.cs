using IvanConnections_Travel.Models.Enums;

namespace IvanConnections_Travel.Utils
{
    public static class Translations
    {
        /// <summary>
        /// Gets the Romanian name for a vehicle type
        /// </summary>
        /// <param name="vehicleType">The vehicle type enum value</param>
        /// <returns>Romanian name for the vehicle type</returns>
        public static string GetVehicleTypeNameInRomanian(VehicleType? vehicleType)
        {
            return vehicleType switch
            {
                VehicleType.Tram => "tramvai",
                VehicleType.Bus => "autobuz",
                VehicleType.Trolleybus => "troleibuz",
                VehicleType.Subway => "metrou",
                VehicleType.Rail => "tren",
                VehicleType.Ferry => "feribot",
                VehicleType.CableCar => "telecabină",
                VehicleType.Gondola => "gondolă",
                VehicleType.Funicular => "funicular",
                VehicleType.Monorail => "monorail",
                _ => "vehicul"
            };
        }

        /// <summary>
        /// Gets the Romanian answer ("Da"/"Nu") for an accessibility value returned by the API.
        /// </summary>
        /// <param name="accessibilityValue">The raw accessibility value (e.g. BIKE_ACCESSIBLE).</param>
        /// <returns>"Da" if accessible, otherwise "Nu".</returns>
        public static string GetAccessibilityInRomanian(string? accessibilityValue)
        {
            return IsAccessible(accessibilityValue) ? "Da" : "Nu";
        }

        /// <summary>
        /// True if the accessibility value returned by the API means that the feature is available.
        /// </summary>
        /// <param name="accessibilityValue">The raw accessibility value (e.g. WHEELCHAIR_ACCESSIBLE).</param>
        public static bool IsAccessible(string? accessibilityValue)
        {
            if (string.IsNullOrWhiteSpace(accessibilityValue)) return false;

            var value = accessibilityValue.Trim();
            if (value.Contains("INACCESSIBLE", StringComparison.OrdinalIgnoreCase)) return false;
            if (value.Contains("ACCESSIBLE", StringComparison.OrdinalIgnoreCase)) return true;

            return value is "1" or "true" or "True" or "DA" or "Da" or "da";
        }
    }
}
