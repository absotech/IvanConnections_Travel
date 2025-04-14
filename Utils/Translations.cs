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
        public static string GetVehicleTypeNameInRomanian(VehicleType vehicleType)
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
    }
}
