using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SnowblindModPlayer.Converters;

public class EqualsToVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return Visibility.Collapsed;

        var left = values[0]?.ToString();
        var right = values[1]?.ToString();
        return string.Equals(left, right, StringComparison.Ordinal) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
