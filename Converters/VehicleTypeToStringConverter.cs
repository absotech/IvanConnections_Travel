using IvanConnections_Travel.Models.Enums;
using IvanConnections_Travel.Utils;
using System.Globalization;

namespace IvanConnections_Travel.Converters
{
    public class VehicleTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not VehicleType vehicleType) return string.Empty;
            var translatedName = Translations.GetVehicleTypeNameInRomanian(vehicleType);
            if (!string.IsNullOrEmpty(translatedName))
            {
                return char.ToUpper(translatedName[0]) + translatedName[1..] + ' ';
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}