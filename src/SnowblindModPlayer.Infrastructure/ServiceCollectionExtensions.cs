using Microsoft.Extensions.DependencyInjection;
using SnowblindModPlayer.Core.Services;
using SnowblindModPlayer.Infrastructure.Data;
using SnowblindModPlayer.Infrastructure.Services;

namespace SnowblindModPlayer.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<IAppDataPathService, AppDataPathService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ISingleInstanceService, SingleInstanceService>();
        
        // Register LibraryDbContext
        services.AddSingleton<LibraryDbContext>(sp =>
        {
            var appDataPathService = sp.GetRequiredService<IAppDataPathService>();
            var dbPath = appDataPathService.GetLibraryDbPath();
            return new LibraryDbContext(dbPath);
        });

        services.AddSingleton<ILibraryService, LibraryService>();
        services.AddSingleton<IThumbnailService, ThumbnailService>();  // Use LibVLC-based implementation
        services.AddSingleton<IThumbnailQueueService, ThumbnailQueueService>();
        services.AddSingleton<IImportService, ImportService>();
        services.AddSingleton<ILoggingService, LoggingService>();
        services.AddSingleton<IMonitorService, MonitorService>();
        services.AddSingleton<IPlaybackService, PlaybackService>();

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        try
        {
            // Initialize database schema
            var dbContext = serviceProvider.GetRequiredService<LibraryDbContext>();
            await dbContext.InitializeAsync();

            // Run E1 cleanup - remove entries with non-existent files
            var libraryService = serviceProvider.GetRequiredService<ILibraryService>();
            await libraryService.CleanupOrphanedEntriesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Database initialization failed: {ex.Message}");
            throw;
        }
    }
}
