using SnowblindModPlayer.Core.Services;

namespace SnowblindModPlayer.Infrastructure.Services;

public class SettingsService : ISettingsService
{
    private readonly Dictionary<string, object> _settings = new();
    private readonly IAppDataPathService _appDataPathService;

    public SettingsService(IAppDataPathService appDataPathService)
    {
        _appDataPathService = appDataPathService;
    }

    public T Get<T>(string key, T defaultValue)
    {
        if (_settings.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }

    public void Set<T>(string key, T value)
    {
        _settings[key] = value!;
    }

    public Task SaveAsync()
    {
        // TODO: Implement JSON serialization
        return Task.CompletedTask;
    }

    public Task LoadAsync()
    {
        // TODO: Implement JSON deserialization and apply defaults
        return Task.CompletedTask;
    }

    public void RegisterLiveUpdate<T>(string key, Action<T> callback)
    {
        // TODO: Implement observable pattern for live updates
    }
}
