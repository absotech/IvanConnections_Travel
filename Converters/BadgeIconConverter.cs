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
                "icon-bus" => "\ue80b",
                "icon-hourglass" => "\ue800",
                "icon-crown" => "\ue844",
                "icon-briefcase" => "\ue801",
                "icon-map-o" => "\uf278",
                "icon-bell-alt" => "\uf0f3",
                "icon-users" => "\ue80a",
                _ => "\uf074"
            };
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}