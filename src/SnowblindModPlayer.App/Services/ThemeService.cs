using Microsoft.Win32;
using SnowblindModPlayer.Core.Services;
using SnowblindModPlayer.Infrastructure.Services;
using System.Linq;
using System.Windows;

namespace SnowblindModPlayer.Services;

public static class ThemeService
{
    private const string PersonalizeKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightThemeValueName = "AppsUseLightTheme";

    public static bool IsWindowsLightTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKeyPath);
            var value = key?.GetValue(AppsUseLightThemeValueName);
            if (value is int intValue)
            {
                return intValue != 0;
            }
        }
        catch
        {
        }

        return false;
    }

    public static bool ResolveIsLightTheme(ISettingsService settings)
    {
        var pref = settings.GetThemePreference();
        return pref switch
        {
            "Light" => true,
            "Dark" => false,
            _ => IsWindowsLightTheme(),
        };
    }

    public static Uri GetThemeDictionaryUri(bool lightTheme)
        => new(lightTheme ? "Themes/Theme.Light.xaml" : "Themes/Theme.Dark.xaml", UriKind.Relative);

    public static void ApplyTheme(Application app, bool lightTheme)
    {
        var newUri = GetThemeDictionaryUri(lightTheme);
        System.Diagnostics.Debug.WriteLine($"ThemeService.ApplyTheme: lightTheme={lightTheme}, uri={newUri}");

        var merged = app.Resources.MergedDictionaries;
        if (merged.Count == 0)
        {
            merged.Add(new ResourceDictionary { Source = newUri });
            return;
        }

        // Find theme dictionary by a known key rather than relying on index.
        var themeIndex = -1;
        for (var i = 0; i < merged.Count; i++)
        {
            if (merged[i].Contains("App.Bg0") || merged[i].Contains("Brush.Bg0"))
            {
                themeIndex = i;
                break;
            }
        }

        if (themeIndex == -1)
        {
            merged.Insert(0, new ResourceDictionary { Source = newUri });
            return;
        }

        // Replace dictionary instance to ensure resource change propagation.
        merged.RemoveAt(themeIndex);
        merged.Insert(themeIndex, new ResourceDictionary { Source = newUri });
    }
}
