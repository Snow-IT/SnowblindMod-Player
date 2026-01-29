namespace SnowblindModPlayer.Core.Services;

public interface IAutostartService
{
    bool IsEnabled();
    Task EnableAsync();
    Task DisableAsync();
}
