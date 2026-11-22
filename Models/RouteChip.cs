namespace IvanConnections_Travel.Models;

/// <summary>
/// A colored route "chip" shown in the map search suggestions / quick routes row.
/// </summary>
public sealed record RouteChip(string ShortName, string? ColorHex)
{
    public Color Background { get; } = ParseColor(ColorHex);

    public Color Foreground => Background.GetLuminosity() > 0.6f ? Colors.Black : Colors.White;

    private static Color ParseColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return Color.FromArgb("#3B82F6");
        var value = hex.StartsWith('#') ? hex : "#" + hex;
        return Color.TryParse(value, out var color) ? color : Color.FromArgb("#3B82F6");
    }
}
