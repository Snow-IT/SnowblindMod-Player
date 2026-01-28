using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using SnowblindModPlayer.ViewModels;

namespace SnowblindModPlayer.Converters;

public class LogLevelBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not LogEntryLevel level)
            return Application.Current.TryFindResource("Brush.LogInfo") as Brush ?? Brushes.Black;

        return level switch
        {
            LogEntryLevel.Debug => Application.Current.TryFindResource("Brush.LogDebug") as Brush ?? Brushes.Gray,
            LogEntryLevel.Warn => Application.Current.TryFindResource("Brush.LogWarn") as Brush ?? Brushes.Goldenrod,
            LogEntryLevel.Error => Application.Current.TryFindResource("Brush.LogError") as Brush ?? Brushes.IndianRed,
            LogEntryLevel.Critical => Application.Current.TryFindResource("Brush.LogCritical") as Brush ?? Brushes.DarkRed,
            _ => Application.Current.TryFindResource("Brush.LogInfo") as Brush ?? Brushes.Black
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
