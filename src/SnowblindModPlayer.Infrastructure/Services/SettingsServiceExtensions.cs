using SnowblindModPlayer.Core.Services;

namespace SnowblindModPlayer.Infrastructure.Services;

public static class SettingsServiceExtensions
{
    private const string MediaFolderKey = "MediaFolder";
    private const string DefaultVideoIdKey = "DefaultVideoId";
    private const string SelectedMonitorIdKey = "SelectedMonitorId";
    private const string VolumeKey = "Volume";
    private const string MutedKey = "Muted";
    private const string LoopEnabledKey = "LoopEnabled";
    private const string FillScreenKey = "FillScreen";
    private const string FullscreenOnStartKey = "FullscreenOnStart";
    private const string ScalingModeKey = "ScalingMode";
    private const string ThemePreferenceKey = "ThemePreference";
    private const string VideosViewModeKey = "VideosViewMode";
    private const string AutostartEnabledKey = "AutostartEnabled";
    private const string AutoplayEnabledKey = "AutoplayEnabled";
    private const string StartDelaySecondsKey = "StartDelaySeconds";
    private const string LoggingLevelKey = "LoggingLevel";
    private const string TrayCloseHintEnabledKey = "TrayCloseHintEnabled";
    private const string SidebarCollapsedKey = "SidebarCollapsed";
    private const string LanguageModeKey = "LanguageMode";
    private const string FixedLanguageKey = "FixedLanguage";
    private const string MinimizeToTrayOnStartupKey = "MinimizeToTrayOnStartup";

    public static string GetMediaFolder(this ISettingsService settings)
    {
        return settings.Get(MediaFolderKey, GetDefaultMediaFolder());
    }

    public static void SetMediaFolder(this ISettingsService settings, string path)
    {
        settings.Set(MediaFolderKey, path);
    }

    public static string GetDefaultVideoId(this ISettingsService settings)
    {
        return settings.Get(DefaultVideoIdKey, string.Empty);
    }

    public static void SetDefaultVideoId(this ISettingsService settings, string videoId)
    {
        settings.Set(DefaultVideoIdKey, videoId);
    }

    public static string GetSelectedMonitorId(this ISettingsService settings)
    {
        return settings.Get(SelectedMonitorIdKey, string.Empty);
    }

    public static void SetSelectedMonitorId(this ISettingsService settings, string monitorId)
    {
        settings.Set(SelectedMonitorIdKey, monitorId);
    }

    public static int GetVolume(this ISettingsService settings)
    {
        return settings.Get(VolumeKey, 50);
    }

    public static void SetVolume(this ISettingsService settings, int volume)
    {
        settings.Set(VolumeKey, Math.Clamp(volume, 0, 100));
    }

    public static bool GetMuted(this ISettingsService settings)
    {
        return settings.Get(MutedKey, false);
    }

    public static void SetMuted(this ISettingsService settings, bool muted)
    {
        settings.Set(MutedKey, muted);
    }

    public static bool GetLoopEnabled(this ISettingsService settings)
    {
        return settings.Get(LoopEnabledKey, true);
    }

    public static void SetLoopEnabled(this ISettingsService settings, bool enabled)
    {
        settings.Set(LoopEnabledKey, enabled);
    }

    public static bool GetFillScreen(this ISettingsService settings)
    {
        return settings.Get(FillScreenKey, true);
    }

    public static void SetFillScreen(this ISettingsService settings, bool fill)
    {
        settings.Set(FillScreenKey, fill);
    }

    public static string GetThemePreference(this ISettingsService settings)
    {
        return settings.Get(ThemePreferenceKey, "System");
    }

    public static void SetThemePreference(this ISettingsService settings, string preference)
    {
        settings.Set(ThemePreferenceKey, preference);
    }

    public static string GetVideosViewMode(this ISettingsService settings)
    {
        return settings.Get(VideosViewModeKey, "Tile");
    }

    public static void SetVideosViewMode(this ISettingsService settings, string mode)
    {
        settings.Set(VideosViewModeKey, mode);
    }

    public static bool GetFullscreenOnStart(this ISettingsService settings)
    {
        return settings.Get(FullscreenOnStartKey, true);
    }

    public static void SetFullscreenOnStart(this ISettingsService settings, bool fullscreen)
    {
        settings.Set(FullscreenOnStartKey, fullscreen);
    }

    public static string GetScalingMode(this ISettingsService settings)
    {
        return settings.Get(ScalingModeKey, "Fill");
    }

    public static void SetScalingMode(this ISettingsService settings, string mode)
    {
        settings.Set(ScalingModeKey, mode);
    }

    private static string GetDefaultMediaFolder()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appDataPath, "SnowblindModPlayer", "media");
    }

    public static bool GetAutostartEnabled(this ISettingsService settings)
    {
        return settings.Get(AutostartEnabledKey, false);
    }

    public static void SetAutostartEnabled(this ISettingsService settings, bool enabled)
    {
        settings.Set(AutostartEnabledKey, enabled);
    }

    public static bool GetAutoplayEnabled(this ISettingsService settings)
    {
        return settings.Get(AutoplayEnabledKey, false);
    }

    public static void SetAutoplayEnabled(this ISettingsService settings, bool enabled)
    {
        settings.Set(AutoplayEnabledKey, enabled);
    }

    public static int GetStartDelaySeconds(this ISettingsService settings)
    {
        return settings.Get(StartDelaySecondsKey, 0);
    }

    public static void SetStartDelaySeconds(this ISettingsService settings, int seconds)
    {
        settings.Set(StartDelaySecondsKey, Math.Max(0, seconds));
    }

    public static string GetLoggingLevel(this ISettingsService settings)
    {
        return settings.Get(LoggingLevelKey, "Warn");
    }

    public static void SetLoggingLevel(this ISettingsService settings, string level)
    {
        settings.Set(LoggingLevelKey, level);
    }

    public static bool GetTrayCloseHintEnabled(this ISettingsService settings)
    {
        return settings.Get(TrayCloseHintEnabledKey, true);
    }

    public static void SetTrayCloseHintEnabled(this ISettingsService settings, bool enabled)
    {
        settings.Set(TrayCloseHintEnabledKey, enabled);
    }

    public static bool GetSidebarCollapsed(this ISettingsService settings)
    {
        return settings.Get(SidebarCollapsedKey, false);
    }

    public static void SetSidebarCollapsed(this ISettingsService settings, bool collapsed)
    {
        settings.Set(SidebarCollapsedKey, collapsed);
    }

    public static string GetLanguageMode(this ISettingsService settings)
    {
        return settings.Get(LanguageModeKey, "System");
    }

    public static void SetLanguageMode(this ISettingsService settings, string mode)
    {
        settings.Set(LanguageModeKey, mode);
    }

    public static string GetFixedLanguage(this ISettingsService settings)
    {
        return settings.Get(FixedLanguageKey, "en-US");
    }

    public static void SetFixedLanguage(this ISettingsService settings, string language)
    {
        settings.Set(FixedLanguageKey, language);
    }

    public static bool GetMinimizeToTrayOnStartup(this ISettingsService settings)
    {
        return settings.Get(MinimizeToTrayOnStartupKey, false);
    }

    public static void SetMinimizeToTrayOnStartup(this ISettingsService settings, bool value)
    {
        settings.Set(MinimizeToTrayOnStartupKey, value);
    }

    public static int GetAutoplayDelaySeconds(this ISettingsService settings)
    {
        return settings.Get(StartDelaySecondsKey, 0);
    }

    public static void SetAutoplayDelaySeconds(this ISettingsService settings, int seconds)
    {
        settings.Set(StartDelaySecondsKey, Math.Max(0, seconds));
    }
}
