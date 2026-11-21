using IvanConnections_Travel.Models.Enums;

namespace IvanConnections_Travel.Utils
{
    /// <summary>
    /// Pictograme (Material Symbols Rounded) și culori de accent pentru tipurile de vehicule,
    /// folosite în popup-urile de vehicul și de stație.
    /// </summary>
    public static class VehicleVisuals
    {
        public const string GenericGlyph = "\ue535";

        /// <summary>
        /// Pictograma Material Symbols potrivită pentru tipul de vehicul.
        /// </summary>
        public static string GetGlyph(VehicleType? vehicleType) => vehicleType switch
        {
            VehicleType.Tram => "\ue571",
            VehicleType.Bus => "\ue530",
            VehicleType.Trolleybus => "\ue530",
            VehicleType.Subway => "\ue56f",
            VehicleType.Rail => "\ue570",
            VehicleType.Monorail => "\ue570",
            VehicleType.Ferry => "\ue532",
            _ => GenericGlyph
        };

        /// <summary>
        /// Culoarea de accent folosită atunci când traseul nu are o culoare proprie.
        /// </summary>
        public static Color GetAccentColor(VehicleType? vehicleType) => vehicleType switch
        {
            VehicleType.Tram => Color.FromArgb("#0F9D58"),
            VehicleType.Bus => Color.FromArgb("#5763FF"),
            VehicleType.Trolleybus => Color.FromArgb("#0EA5E9"),
            VehicleType.Subway => Color.FromArgb("#7C3AED"),
            VehicleType.Rail or VehicleType.Monorail => Color.FromArgb("#B45309"),
            VehicleType.Ferry => Color.FromArgb("#0891B2"),
            _ => Color.FromArgb("#5763FF")
        };
    }
}
