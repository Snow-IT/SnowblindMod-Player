using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SnowblindModPlayer.Core.Services;
using SnowblindModPlayer.Infrastructure;
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
            try
            {
                _serviceProvider.InitializeDatabaseAsync().Wait();
                System.Diagnostics.Debug.WriteLine("? Database initialized");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Database initialization failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }

            // Create main window WITHOUT loading settings first
            System.Diagnostics.Debug.WriteLine("Creating main window...");
            try
            {
                MainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                System.Diagnostics.Debug.WriteLine("? MainWindow obtained");

                var viewModel = _serviceProvider.GetRequiredService<SnowblindModPlayer.ViewModels.MainWindowViewModel>();
                System.Diagnostics.Debug.WriteLine("? MainWindowViewModel obtained");

                MainWindow.DataContext = viewModel;
                System.Diagnostics.Debug.WriteLine("? DataContext set");

                MainWindow.Show();
                System.Diagnostics.Debug.WriteLine("? Main window shown");

                // Load settings AFTER window is shown
                System.Diagnostics.Debug.WriteLine("Loading settings (async)...");
                var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
                // Don't wait - load asynchronously after window is shown
                #pragma warning disable CS4014
                settingsService.LoadAsync();
                #pragma warning restore CS4014
                System.Diagnostics.Debug.WriteLine("? Settings load initiated");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Window creation failed: {ex}");
                System.Diagnostics.Debug.WriteLine($"InnerException: {ex.InnerException}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
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
    }
}
