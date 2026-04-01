using System.Globalization;

namespace IvanConnections_Travel.Converters;

public class GlowConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not int order)
            return Colors.Transparent;

        return order switch
        {
            5 => Color.FromArgb("#FFD700"),
            4 => Color.FromArgb("#9C27B0"),
            3 => Color.FromArgb("#2196F3"),
            _ => Colors.Transparent
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
}