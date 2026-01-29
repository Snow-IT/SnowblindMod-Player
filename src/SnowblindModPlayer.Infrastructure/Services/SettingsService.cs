using System.Text.Json;
using SnowblindModPlayer.Core.Services;

namespace SnowblindModPlayer.Infrastructure.Services;

public class SettingsService : ISettingsService
{
    private readonly Dictionary<string, object> _settings = new();
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly IAppDataPathService _appDataPathService;
    private readonly string _settingsFilePath;
    private readonly Dictionary<string, List<Delegate>> _liveUpdateCallbacks = new();

    // Default settings
    private static readonly Dictionary<string, object> DefaultSettings = new()
    {
        { "MediaFolder", GetDefaultMediaFolder() },
        { "SelectedMonitorId", string.Empty },
        { "DefaultVideoId", string.Empty },
        { "ThemePreference", "System" },
        { "VideosViewMode", "List" },
        { "LoopEnabled", true },
        { "Volume", 50 },
        { "Muted", true },
        { "FullscreenOnStart", true },
        { "ScalingMode", "Fill" },
        { "AutostartEnabled", false },
        { "AutoplayEnabled", true },
        { "StartDelaySeconds", 0 },
        { "MinimizeToTrayOnStartup", false },
        { "LoggingLevel", "Warning" },
        { "TrayCloseHintEnabled", true },
        { "SidebarCollapsed", true },
        { "LanguageMode", "System" },
        { "FixedLanguage", "en-US" },
    };

    public SettingsService(IAppDataPathService appDataPathService)
    {
        _appDataPathService = appDataPathService;
        _settingsFilePath = appDataPathService.GetSettingsFilePath();
    }

    public T Get<T>(string key, T defaultValue)
    {
        if (_settings.TryGetValue(key, out var value))
        {
            if (value is T typedValue)
            {
                return typedValue;
            }

            // Handle JSON element conversion
            if (value is JsonElement jsonElement)
            {
                try
                {
                    return JsonSerializer.Deserialize<T>(jsonElement.GetRawText()) ?? defaultValue;
                }
                catch
                {
                    return defaultValue;
                }
            }
        }

        // Try to get default value
        if (DefaultSettings.TryGetValue(key, out var defaultVal) && defaultVal is T defaultTypedVal)
        {
            return defaultTypedVal;
        }

        return defaultValue;
    }

    public void Set<T>(string key, T value)
    {
        var oldValue = _settings.ContainsKey(key) ? _settings[key] : null;
        _settings[key] = value!;

        // Invoke live update callbacks
        if (_liveUpdateCallbacks.TryGetValue(key, out var callbacks))
        {
            foreach (var callback in callbacks)
            {
                try
                {
                    if (callback is Action<T> typedCallback)
                    {
                        typedCallback(value);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Live update callback failed for {key}: {ex.Message}");
                }
            }
        }
    }

    public async Task SaveAsync()
    {
        try
        {
            var stable = new Dictionary<string, JsonElement>();
            foreach (var kvp in _settings)
            {
                stable[kvp.Key] = kvp.Value switch
                {
                    JsonElement je => je,
                    _ => JsonSerializer.SerializeToElement(kvp.Value, JsonOptions)
                };
            }

            var json = JsonSerializer.Serialize(stable, JsonOptions);
            await File.WriteAllTextAsync(_settingsFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    public async Task LoadAsync()
    {
        try
        {
            // First load defaults
            foreach (var kvp in DefaultSettings)
            {
                if (!_settings.ContainsKey(kvp.Key))
                {
                    _settings[kvp.Key] = kvp.Value;
                }
            }

            // Then override with persisted values
            if (File.Exists(_settingsFilePath))
            {
                var json = await File.ReadAllTextAsync(_settingsFilePath);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in doc.RootElement.EnumerateObject())
                        _settings[prop.Name] = prop.Value.Clone();
                }
            }

            if (_settings.TryGetValue("ThemePreference", out var themePref))
                System.Diagnostics.Debug.WriteLine($"Loaded ThemePreference={themePref}");

        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
        }
    }

    public void RegisterLiveUpdate<T>(string key, Action<T> callback)
    {
        if (!_liveUpdateCallbacks.ContainsKey(key))
        {
            _liveUpdateCallbacks[key] = new List<Delegate>();
        }

        _liveUpdateCallbacks[key].Add(callback);
    }

    private static string GetDefaultMediaFolder()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appDataPath, "SnowblindModPlayer", "media");
    }
}
