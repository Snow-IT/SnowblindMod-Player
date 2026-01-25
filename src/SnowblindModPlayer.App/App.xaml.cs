using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SnowblindModPlayer.Core.Services;
using SnowblindModPlayer.Infrastructure;
using SnowblindModPlayer.UI.ViewModels;

namespace SnowblindModPlayer;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Build DI container
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // Initialize AppData paths
        var pathService = _serviceProvider.GetRequiredService<IAppDataPathService>();
        pathService.EnsureDirectoriesExist();

        // Load settings
        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        settingsService.LoadAsync().Wait();

        // Set main window
        MainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        MainWindow.DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>();
        MainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Save settings before exit
        if (_serviceProvider?.GetRequiredService<ISettingsService>() is ISettingsService settingsService)
        {
            settingsService.SaveAsync().Wait();
        }

        _serviceProvider?.Dispose();
        base.OnExit(e);
    }

    private static void ConfigureServices(ServiceCollection services)
    {
        // Register infrastructure services
        services.AddInfrastructureServices();

        // Register UI services
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();
    }
}
