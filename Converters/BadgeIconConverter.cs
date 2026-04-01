using System.Globalization;

namespace IvanConnections_Travel.Converters;

public class BadgeIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string iconCode)
        {
            return iconCode.ToLower() switch
            {
                "icon-message" => "\uf4ac",
                "icon-explorer" => "\ue801",
                "icon-star" => "\ue802",
                _ => "\ue800"
            };
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}