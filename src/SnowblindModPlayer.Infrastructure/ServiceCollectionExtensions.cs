using Microsoft.Extensions.DependencyInjection;
using SnowblindModPlayer.Core.Services;
using SnowblindModPlayer.Infrastructure.Services;

namespace SnowblindModPlayer.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<IAppDataPathService, AppDataPathService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ILibraryService, LibraryService>();
        services.AddSingleton<IThumbnailService, ThumbnailService>();
        services.AddSingleton<ILoggingService, LoggingService>();
        services.AddSingleton<IMonitorService, MonitorService>();
        services.AddSingleton<IPlaybackService, PlaybackService>();

        return services;
    }
}
