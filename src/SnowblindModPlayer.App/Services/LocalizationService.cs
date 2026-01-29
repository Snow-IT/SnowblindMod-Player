using System.Globalization;
using System.Linq;
using System.Windows;
using SnowblindModPlayer.Core.Services;
using SnowblindModPlayer.Infrastructure.Services;

namespace SnowblindModPlayer.Services;

public static class LocalizationService
{
    public static void ApplyLanguage(Application app, ISettingsService settingsService)
    {
        var mode = settingsService.GetLanguageMode();
        var culture = ResolveCulture(mode);
        var source = culture.StartsWith("de", StringComparison.OrdinalIgnoreCase)
            ? "Resources/Strings.de.xaml"
            : "Resources/Strings.en.xaml";

        var dictionary = new ResourceDictionary { Source = new Uri(source, UriKind.Relative) };
        var merged = app.Resources.MergedDictionaries;
        var existing = merged.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Resources/Strings."));
        if (existing != null)
        {
            merged.Remove(existing);
        }
        merged.Add(dictionary);
    }

    private static string ResolveCulture(string mode)
    {
        return mode switch
        {
            "English" => "en",
            "German" => "de",
            _ => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
        };
    }
}
