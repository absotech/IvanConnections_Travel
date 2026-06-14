using System.Globalization;

namespace IvanConnections_Travel.Converters;

/// <summary>
/// Returns true when the value is null, false otherwise.
/// Pass ConverterParameter="invert" to reverse the result.
/// </summary>
public class NullToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isNull = value is null;
        return parameter?.ToString() == "invert" ? !isNull : isNull;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
