using System.Text.Json;
using SnowblindModPlayer.Core.Services;

namespace SnowblindModPlayer.Infrastructure.Services;

public class SettingsService : ISettingsService
{
    private readonly Dictionary<string, object> _settings = new();
    private readonly IAppDataPathService _appDataPathService;
    private readonly string _settingsFilePath;
    private readonly Dictionary<string, List<Delegate>> _liveUpdateCallbacks = new();

    // Default settings
    private static readonly Dictionary<string, object> DefaultSettings = new()
    {
        { "MediaFolder", GetDefaultMediaFolder() },
        { "SelectedMonitorId", string.Empty },
        { "DefaultVideoId", string.Empty },
        { "LoopEnabled", true },
        { "FillScreen", true },
        { "KeepAspectRatio", false },
        { "Volume", 50 },
        { "Muted", false },
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
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
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
                var loaded = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                if (loaded != null)
                {
                    foreach (var kvp in loaded)
                    {
                        _settings[kvp.Key] = kvp.Value;
                    }
                }
            }
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
