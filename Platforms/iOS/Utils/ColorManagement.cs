using UIKit;

namespace IvanConnections_Travel.Utils
{
    public static partial class ColorManagement
    {
        public static bool IsColorDark(UIColor color)
        {
            color.GetRGBA(out nfloat red, out nfloat green, out nfloat blue, out _);
            
            double brightness = (0.299 * red + 0.587 * green + 0.114 * blue);
            return brightness < 0.5;
        }

        public static UIColor ParseColor(string colorHex)
        {
            colorHex = NormalizeColorHex(colorHex);

            if (colorHex.Length == 7 && colorHex.StartsWith("#"))
            {
                var r = Convert.ToInt32(colorHex.Substring(1, 2), 16) / 255f;
                var g = Convert.ToInt32(colorHex.Substring(3, 2), 16) / 255f;
                var b = Convert.ToInt32(colorHex.Substring(5, 2), 16) / 255f;
                return UIColor.FromRGB(r, g, b);
            }

            return UIColor.Black;
        }
    }
}
