using System.Globalization;
using System.Windows.Data;

namespace SnowblindModPlayer.Converters;

public sealed class PageToTagConverter : IValueConverter
{
    public string PageName { get; set; } = string.Empty;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var currentPage = value as string;
        return string.Equals(currentPage, PageName, StringComparison.OrdinalIgnoreCase) 
            ? "Selected" 
            : "NotSelected";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
