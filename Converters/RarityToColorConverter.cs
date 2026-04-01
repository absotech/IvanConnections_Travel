using System.Globalization;

namespace IvanConnections_Travel.Converters;

public class RarityToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int order)
        {
            return order switch
            {
                1 => Color.FromArgb("#9E9E9E"),
                2 => Color.FromArgb("#4CAF50"),
                3 => Color.FromArgb("#2196F3"),
                4 => Color.FromArgb("#9C27B0"),
                5 => Color.FromArgb("#FFD700"),
                _ => Color.FromArgb("#EEEEEE")
            };
        }
        return Color.FromArgb("#EEEEEE");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
}