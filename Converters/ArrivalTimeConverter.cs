using System.Globalization;

namespace IvanConnections_Travel.Converters;
public class ArrivalTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int minutes)
        {
            if (minutes == -1)
                return "PE CAPĂT";

            if (minutes == 0)
                return "Sosire";

            return $"{minutes} min";
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

