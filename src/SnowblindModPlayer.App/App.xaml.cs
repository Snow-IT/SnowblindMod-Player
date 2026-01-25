using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SnowblindModPlayer.Core.Services;
using SnowblindModPlayer.Infrastructure;
using SnowblindModPlayer.Infrastructure.Services;
using SnowblindModPlayer.Services;
using SnowblindModPlayer.UI.ViewModels;
using SnowblindModPlayer.ViewModels;
using SnowblindModPlayer.Views;

namespace SnowblindModPlayer;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            System.Diagnostics.Debug.WriteLine("=== App Startup Started ===");

            // Build DI container
            System.Diagnostics.Debug.WriteLine("Building DI container...");
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
            System.Diagnostics.Debug.WriteLine("? DI container built");

            // Initialize AppData paths
            System.Diagnostics.Debug.WriteLine("Initializing AppData paths...");
            var pathService = _serviceProvider.GetRequiredService<IAppDataPathService>();
            pathService.EnsureDirectoriesExist();
            System.Diagnostics.Debug.WriteLine("? AppData paths initialized");

            // Initialize database and run migrations
            System.Diagnostics.Debug.WriteLine("Initializing database...");
            _serviceProvider.InitializeDatabaseAsync().GetAwaiter().GetResult();
            System.Diagnostics.Debug.WriteLine("? Database initialized");

            var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();

            // Create main window
            System.Diagnostics.Debug.WriteLine("Creating main window...");
            MainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            var viewModel = _serviceProvider.GetRequiredService<SnowblindModPlayer.ViewModels.MainWindowViewModel>();
            MainWindow.DataContext = viewModel;
            MainWindow.Show();
            System.Diagnostics.Debug.WriteLine("? Main window shown");

            // Load settings + apply theme without blocking UI thread
            _ = Task.Run(async () =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("Loading settings...");
                    var loadTask = settingsService.LoadAsync();
                    var completed = await Task.WhenAny(loadTask, Task.Delay(TimeSpan.FromSeconds(3)));
                    if (completed != loadTask)
                    {
                        System.Diagnostics.Debug.WriteLine("? Settings load timed out after 3s; continuing with defaults");
                        return;
                    }

                    System.Diagnostics.Debug.WriteLine("? Settings loaded");
                    Dispatcher.Invoke(() =>
                    {
                        System.Diagnostics.Debug.WriteLine("Applying theme...");
                        ThemeService.ApplyTheme(this, ThemeService.ResolveIsLightTheme(settingsService));
                        System.Diagnostics.Debug.WriteLine("? Theme applied");
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"? Settings load failed: {ex}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Application startup failed: {ex}");
            System.Diagnostics.Debug.WriteLine($"InnerException: {ex.InnerException}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            
            MessageBox.Show($"Application startup failed:\n\n{ex.Message}\n\nInnerException: {ex.InnerException?.Message}", 
                "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
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

        // Register UI services and ViewModels
        services.AddSingleton<MainWindow>();
        services.AddSingleton<SnowblindModPlayer.ViewModels.MainWindowViewModel>();
        services.AddSingleton<PlayerWindow>();
        services.AddSingleton<PlayerWindowViewModel>();
        services.AddSingleton<MonitorSelectionViewModel>();

        // Register Views
        services.AddSingleton<VideosView>();
        services.AddSingleton<LogsView>();
        services.AddSingleton<SettingsView>();

        // Register ViewModels
        services.AddSingleton<SnowblindModPlayer.ViewModels.VideosViewModel>();
    }
}
