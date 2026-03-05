using System.Globalization;

namespace IvanConnections_Travel.Converters;
public class ArrivalTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double minutes) return string.Empty;
        return minutes switch
        {
            -1 => "PE CAPĂT",
            0 => "Sosire",
            _ => $"{minutes} min"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

