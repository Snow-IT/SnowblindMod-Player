using System.Globalization;
using System.Windows.Data;

namespace SnowblindModPlayer.Converters;

public class MultiplyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double doubleValue && parameter is string parameterString)
        {
            if (double.TryParse(parameterString, CultureInfo.InvariantCulture, out var multiplier))
            {
                return doubleValue * multiplier;
            }
        }

        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
