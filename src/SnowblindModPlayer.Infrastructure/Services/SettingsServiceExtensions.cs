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
    private const string ThemePreferenceKey = "ThemePreference";
    private const string VideosViewModeKey = "VideosViewMode";

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

    private static string GetDefaultMediaFolder()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appDataPath, "SnowblindModPlayer", "media");
    }
}
