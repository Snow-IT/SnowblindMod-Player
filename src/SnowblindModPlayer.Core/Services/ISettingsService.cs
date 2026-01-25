namespace SnowblindModPlayer.Core.Services;

public interface ISettingsService
{
    T Get<T>(string key, T defaultValue);
    void Set<T>(string key, T value);
    Task SaveAsync();
    Task LoadAsync();
    void RegisterLiveUpdate<T>(string key, Action<T> callback);
}
