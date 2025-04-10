using Color = Android.Graphics.Color;

namespace IvanConnections_Travel.Utils
{
    public static class ColorManagement
    {
        public static bool IsColorDark(Color color)
        {
            double brightness = (0.299 * Color.GetRedComponent(color) +
                                0.587 * Color.GetGreenComponent(color) +
                                0.114 * Color.GetBlueComponent(color)) / 255;

            return brightness < 0.5;
        }
        public static string NormalizeColorHex(string colorHex)
        {
            if (string.IsNullOrEmpty(colorHex))
                return "#FFFFFF";

            if (!colorHex.StartsWith("#"))
                colorHex = "#" + colorHex;

            if (colorHex.Length == 4)
            {
                var r = colorHex[1];
                var g = colorHex[2];
                var b = colorHex[3];
                colorHex = $"#{r}{r}{g}{g}{b}{b}";
            }
            if (colorHex.Length != 7)
            {
                System.Diagnostics.Debug.WriteLine($"Invalid color format: {colorHex}, defaulting to #000000");
                return "#000000";
            }

            return colorHex;
        }
    }
}
