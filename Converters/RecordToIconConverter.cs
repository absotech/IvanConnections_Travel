using IvanConnections_Travel.Models;
using Microsoft.Maui.Controls.Shapes;
using System.Globalization;
using Path = Microsoft.Maui.Controls.Shapes.Path;

namespace IvanConnections_Travel.Converters
{
    public class RecordToIconConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
            {
                return null;
            }

            if (value is not Record record)
            {
                throw new InvalidOperationException("value is not a Record");
            }

            return GeneratePathIcon(record, Colors.Black);
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();

        public static Path GeneratePathIcon(Record record, Color color)
        {
            var pathFigureCollection = new PathFigureCollection();
            PathFigureCollectionConverter.ParseStringToPathFigureCollection(pathFigureCollection, record.PathGeometry);
            var pathGeometry = new PathGeometry(pathFigureCollection);

            return new Path(pathGeometry)
            {
                Aspect = Stretch.Uniform,
                HeightRequest = 50,
                WidthRequest = 50,
                Fill = color,
            };
        }
    }
}
