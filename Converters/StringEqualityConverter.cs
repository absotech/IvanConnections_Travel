using System.Globalization;

namespace IvanConnections_Travel.Converters;

/// <summary>
/// Returns true when the bound string value equals the ConverterParameter.
/// </summary>
public class StringEqualityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.OrdinalIgnoreCase);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
