using System.Text.Json;
using SnowblindModPlayer.Core.Services;

namespace SnowblindModPlayer.Infrastructure.Services;

public class SettingsService : ISettingsService
{
    private readonly Dictionary<string, object> _settings = new();
    private readonly IAppDataPathService _appDataPathService;
    private readonly string _settingsFilePath;

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
        return defaultValue;
    }

    public void Set<T>(string key, T value)
    {
        _settings[key] = value!;
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
            if (File.Exists(_settingsFilePath))
            {
                var json = await File.ReadAllTextAsync(_settingsFilePath);
                var loaded = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                if (loaded != null)
                {
                    _settings.Clear();
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
        // TODO: Implement observable pattern for live updates
    }
}
