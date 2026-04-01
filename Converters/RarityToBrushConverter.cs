using System.Globalization;

namespace IvanConnections_Travel.Converters;

public class RarityToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not int order)
            return Default();

        return order switch
        {
            1 => Gray(),
            2 => Green(),
            3 => Blue(),
            4 => Purple(),
            5 => Gold(),
            _ => Default()
        };
    }

    private Brush Gray() => new LinearGradientBrush(
        new GradientStopCollection
        {
            new GradientStop(Color.FromArgb("#E0E0E0"), 0),
            new GradientStop(Color.FromArgb("#9E9E9E"), 1),
        },
        new Point(0, 0), new Point(1, 1));

    private Brush Green() => new LinearGradientBrush(
        new GradientStopCollection
        {
            new GradientStop(Color.FromArgb("#A5D6A7"), 0),
            new GradientStop(Color.FromArgb("#4CAF50"), 1),
        },
        new Point(0, 0), new Point(1, 1));

    private Brush Blue() => new LinearGradientBrush(
        new GradientStopCollection
        {
            new GradientStop(Color.FromArgb("#90CAF9"), 0),
            new GradientStop(Color.FromArgb("#2196F3"), 1),
        },
        new Point(0, 0), new Point(1, 1));

    private Brush Purple() => new LinearGradientBrush(
        new GradientStopCollection
        {
            new GradientStop(Color.FromArgb("#CE93D8"), 0),
            new GradientStop(Color.FromArgb("#9C27B0"), 1),
        },
        new Point(0, 0), new Point(1, 1));

    private Brush Gold() => new LinearGradientBrush(
        new GradientStopCollection
        {
            new GradientStop(Color.FromArgb("#FFF4B0"), 0),
            new GradientStop(Color.FromArgb("#FFD700"), 0.5f),
            new GradientStop(Color.FromArgb("#B89600"), 1),
        },
        new Point(0, 0), new Point(1, 1));

    private Brush Default() => new SolidColorBrush(Color.FromArgb("#EEEEEE"));

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
}
