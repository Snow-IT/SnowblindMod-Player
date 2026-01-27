using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SnowblindModPlayer.Core.Services;
using SnowblindModPlayer.Infrastructure;
using SnowblindModPlayer.Services;
using SnowblindModPlayer.UI.ViewModels;
using SnowblindModPlayer.ViewModels;
using SnowblindModPlayer.Views;

namespace SnowblindModPlayer;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private ITrayService? _trayService;
    private ISingleInstanceService? _singleInstanceService;
    private MainWindow? _mainWindow;
    private bool _shutdownRequested;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Register exception handlers FIRST, before anything else
        DispatcherUnhandledException += (s, args) =>
        {
            System.Diagnostics.Debug.WriteLine($"? UNHANDLED EXCEPTION: {args.Exception}");
            args.Handled = true;
            Shutdown(1);
        };
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            System.Diagnostics.Debug.WriteLine($"? UNHANDLED APPDOMAIN EXCEPTION: {args.ExceptionObject}");
            Shutdown(1);
        };
        
        base.OnStartup(e);

        // Show startup window
        var startupWindow = new StartupWindow();
        startupWindow.Show();

        try
        {
            System.Diagnostics.Debug.WriteLine("=== App Startup Started ===");

            // Build DI container
            System.Diagnostics.Debug.WriteLine("Building DI container...");
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
            System.Diagnostics.Debug.WriteLine("? DI container built");

            // Single instance guard
            var singleInstance = _serviceProvider.GetRequiredService<ISingleInstanceService>();
            _singleInstanceService = singleInstance;
            System.Diagnostics.Debug.WriteLine("? Attempting to acquire primary instance...");
            if (!singleInstance.TryAcquirePrimary())
            {
                System.Diagnostics.Debug.WriteLine("? Another instance detected, notifying primary and exiting");
                singleInstance.NotifyPrimaryInstance();
                Shutdown();
                return;
            }
            System.Diagnostics.Debug.WriteLine("? Primary instance acquired");
            _singleInstanceService.StartListening(() => Dispatcher.Invoke(ShowMainWindowFromTray));

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

            // Load settings async without blocking UI thread
            _ = Task.Run(async () =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("Loading settings...");
                    await settingsService.LoadAsync();
                    System.Diagnostics.Debug.WriteLine("? Settings loaded");

                    // Apply theme and create main window on UI thread
                    await Dispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            System.Diagnostics.Debug.WriteLine("Applying theme...");
                            ThemeService.ApplyTheme(this, ThemeService.ResolveIsLightTheme(settingsService));
                            System.Diagnostics.Debug.WriteLine("? Theme applied");

                            System.Diagnostics.Debug.WriteLine("Creating main window...");
                            _mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                            var viewModel = _serviceProvider.GetRequiredService<SnowblindModPlayer.ViewModels.MainWindowViewModel>();
                            _mainWindow.DataContext = viewModel;

                            // Initialize tray with full menu support
                            _trayService = _serviceProvider.GetRequiredService<ITrayService>();
                            var playbackOrchestrator = _serviceProvider.GetRequiredService<PlaybackOrchestrator>();
                            var libraryService = _serviceProvider.GetRequiredService<ILibraryService>();
                            
                            _trayService.Initialize(
                                onShowRequested: ShowMainWindowFromTray,
                                onExitRequested: ExitFromTray,
                                onPlayDefaultRequested: playbackOrchestrator.PlayDefaultVideoAsync,
                                onPlayVideoRequested: playbackOrchestrator.PlayVideoAsync,
                                onStopRequested: async () =>
                                {
                                    var playbackService = _serviceProvider.GetRequiredService<IPlaybackService>();
                                    await playbackService.StopAsync();
                                    System.Diagnostics.Debug.WriteLine("? Playback stopped from tray");
                                },
                                getVideosForMenu: async () =>
                                {
                                    try
                                    {
                                        var allVideos = await libraryService.GetAllMediaAsync();
                                        var defaultVideo = await libraryService.GetDefaultVideoAsync();

                                        var videos = allVideos
                                            .Select(m => new VideoItem
                                            {
                                                Id = m.Id,
                                                DisplayName = m.DisplayName,
                                                IsDefault = defaultVideo != null && m.Id == defaultVideo.Id
                                            })
                                            .OrderBy(v => v.IsDefault ? 0 : 1)
                                            .ThenBy(v => v.DisplayName)
                                            .ToList();

                                        return videos;
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"? Error fetching videos for tray menu: {ex.Message}");
                                        return new List<VideoItem>();
                                    }
                                });

                            // Close-to-tray handling
                            _mainWindow.Closing += MainWindow_Closing;

                            startupWindow.Close();
                            
                            // Start hidden in tray (per spec: Close-to-tray)
                            // MUST set as MainWindow so WPF has a message loop
                            MainWindow = _mainWindow;
                            _mainWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
                            _mainWindow.ShowInTaskbar = false;
                            _mainWindow.Show(); // required for message loop
                            _mainWindow.Visibility = System.Windows.Visibility.Hidden;
                            System.Diagnostics.Debug.WriteLine("? Main window initialized hidden for tray mode");

                            // Autoplay on startup (SPEC 2.6: Autoplay with startup delay)
                            var autoplayEnabled = settingsService.Get("AutoplayEnabled", false);
                            var autoplayDelayMs = settingsService.Get("AutoplayDelayMs", 0);
                            
                            if (autoplayEnabled)
                            {
                                _ = Task.Run(async () =>
                                {
                                    try
                                    {
                                        if (autoplayDelayMs > 0)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"? Autoplay delayed by {autoplayDelayMs}ms");
                                            await Task.Delay(autoplayDelayMs);
                                        }
                                        
                                        System.Diagnostics.Debug.WriteLine("? Starting autoplay...");
                                        await playbackOrchestrator.PlayDefaultVideoAsync();
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"? Autoplay failed: {ex.Message}");
                                    }
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"? Main window creation failed: {ex}");
                            System.Windows.MessageBox.Show($"Application startup failed:\n\n{ex.Message}", "Fatal Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                            try { startupWindow.Close(); } catch { }
                            Shutdown(1);
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"? Settings load failed: {ex}");
                    await Dispatcher.InvokeAsync(() =>
                    {
                        System.Windows.MessageBox.Show($"Settings load failed:\n\n{ex.Message}\n\nContinuing with defaults.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);

                        try
                        {
                            _mainWindow = _serviceProvider!.GetRequiredService<MainWindow>();
                            var viewModel = _serviceProvider.GetRequiredService<SnowblindModPlayer.ViewModels.MainWindowViewModel>();
                            _mainWindow.DataContext = viewModel;

                             _trayService = _serviceProvider.GetRequiredService<ITrayService>();
                             var playbackOrchestrator = _serviceProvider.GetRequiredService<PlaybackOrchestrator>();
                             var libraryService = _serviceProvider.GetRequiredService<ILibraryService>();
                             
                             _trayService.Initialize(
                                 onShowRequested: ShowMainWindowFromTray,
                                 onExitRequested: ExitFromTray,
                                 onPlayDefaultRequested: playbackOrchestrator.PlayDefaultVideoAsync,
                                 onPlayVideoRequested: playbackOrchestrator.PlayVideoAsync,
                                 onStopRequested: async () =>
                                 {
                                     var playbackService = _serviceProvider.GetRequiredService<IPlaybackService>();
                                     await playbackService.StopAsync();
                                 },
                                 getVideosForMenu: async () =>
                                 {
                                     try
                                     {
                                         var allVideos = await libraryService.GetAllMediaAsync();
                                         var defaultVideo = await libraryService.GetDefaultVideoAsync();

                                         var videos = allVideos
                                             .Select(m => new VideoItem
                                             {
                                                 Id = m.Id,
                                                 DisplayName = m.DisplayName,
                                                 IsDefault = defaultVideo != null && m.Id == defaultVideo.Id
                                             })
                                             .OrderBy(v => v.IsDefault ? 0 : 1)
                                             .ThenBy(v => v.DisplayName)
                                             .ToList();

                                         return videos;
                                     }
                                     catch { return new List<VideoItem>(); }
                                 });

                            // Close-to-tray handling
                            _mainWindow.Closing += MainWindow_Closing;

                            startupWindow.Close();
                            _mainWindow.Show();
                            
                            // Autoplay (fallback path, no delay in error state)
                            var autoplayEnabled = settingsService.Get("AutoplayEnabled", false);
                            if (autoplayEnabled)
                            {
                                _ = Task.Run(async () =>
                                {
                                    try
                                    {
                                        System.Diagnostics.Debug.WriteLine("? Starting autoplay (fallback path)...");
                                        await playbackOrchestrator.PlayDefaultVideoAsync();
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"? Autoplay failed: {ex.Message}");
                                    }
                                });
                            }
                        }
                        catch (Exception innerEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"? Fallback window creation failed: {innerEx}");
                            try { startupWindow.Close(); } catch { }
                            Shutdown(1);
                        }
                    });
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Application startup failed: {ex}");
            System.Windows.MessageBox.Show($"Application startup failed:\n\n{ex.Message}", "Fatal Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            try { startupWindow.Close(); } catch { }
            Shutdown(1);
        }
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_shutdownRequested)
        {
            return;
        }

        // Close-to-tray: cancel closing and hide window
        e.Cancel = true;
        _trayService?.SetMainWindowVisible(false);
    }

    private void ShowMainWindowFromTray()
    {
        _trayService?.SetMainWindowVisible(true);
    }

    private void ExitFromTray()
    {
        _shutdownRequested = true;
        _trayService?.Dispose();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Save settings before exit
        if (_serviceProvider?.GetRequiredService<ISettingsService>() is ISettingsService settingsService)
        {
            settingsService.SaveAsync().Wait();
        }

        _trayService?.Dispose();
        _singleInstanceService?.Dispose();
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
        services.AddTransient<PlayerWindow>();
        services.AddTransient<PlayerWindowViewModel>();
        services.AddSingleton<MonitorSelectionViewModel>();

        // Register Views
        services.AddSingleton<VideosView>();
        services.AddSingleton<LogsView>();
        services.AddSingleton<SettingsView>();

        // Register ViewModels
        services.AddSingleton<SnowblindModPlayer.ViewModels.VideosViewModel>();

        // Tray service
        services.AddSingleton<ITrayService, TrayService>();

        // Unified playback orchestrator (single entry point for all "play video" scenarios)
        services.AddSingleton<PlaybackOrchestrator>();
    }
}
