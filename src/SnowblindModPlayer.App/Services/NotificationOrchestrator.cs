using System.Windows;
using SnowblindModPlayer.Core.Services;

namespace SnowblindModPlayer.Services;

public class NotificationOrchestrator : INotificationOrchestrator
{
    private readonly ITrayService _trayService;
    private readonly ILoggingService _logger;

    public NotificationOrchestrator(ITrayService trayService, ILoggingService logger)
    {
        _trayService = trayService;
        _logger = logger;
    }

    public Task NotifyErrorAsync(string message, Exception? exception = null, NotificationScenario scenario = NotificationScenario.Generic)
    {
        var scenarioName = scenario.ToString();
        _logger.Log(LogLevel.Error, "Notify", $"Error ({scenarioName}): {message}", exception);
        return NotifyAsync(message, scenario, NotificationType.Error);
    }

    /// <summary>
    /// Get MainWindow visibility state for smart routing decisions.
    /// </summary>
    private bool IsMainWindowVisible()
    {
        try
        {
            var mainWindow = Application.Current?.MainWindow as MainWindow;
            return mainWindow != null && 
                   mainWindow.IsVisible && 
                   mainWindow.Visibility == Visibility.Visible &&
                   mainWindow.ShowInTaskbar;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Show banner in main window (visible UI notification).
    /// </summary>
    public Task ShowBannerAsync(string message, NotificationType type = NotificationType.Info, int durationMs = 5000)
    {
        try
        {
            var mainWindow = Application.Current?.MainWindow as MainWindow;
            if (mainWindow != null && mainWindow.IsVisible)
            {
                mainWindow.ShowBanner(message, type, durationMs);
                _logger.Log(LogLevel.Debug, "Notify", $"Banner: {message}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BANNER-ERROR] {ex.Message}");
            _logger.Log(LogLevel.Error, "Notify", $"Banner failed: {ex.Message}", ex);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Show blocking confirmation dialog.
    /// </summary>
    public Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return Task.FromResult(result == MessageBoxResult.Yes);
    }

    /// <summary>
    /// Show error dialog (blocking).
    /// </summary>
    public Task ShowErrorAsync(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Show Windows native toast notification (via Tray).
    /// </summary>
    public Task ShowTrayToastAsync(string title, string message, NotificationType type = NotificationType.Info, int durationMs = 6000)
    {
        try
        {
            _trayService.ShowNotification(title, message, type);
            _logger.Log(LogLevel.Debug, "Notify", $"Toast: {title} - {message}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TOAST-ERROR] {ex.Message}");
            _logger.Log(LogLevel.Error, "Notify", $"Toast failed: {ex.Message}", ex);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Smart routing: NotifyAsync selects optimal display method based on:
    /// - MainWindow visibility
    /// - Notification type (Error, Warning, Info, Success)
    /// - Scenario context (Autoplay, Import, Playback, etc.)
    /// 
    /// Routing logic:
    /// - If MainWindow visible: Show Banner (non-blocking, less intrusive)
    /// - If MainWindow hidden: Show Toast (visible even if app is tray)
    /// - Special cases (MinimizeToTray): Always Toast
    /// </summary>
    public async Task NotifyAsync(string message, NotificationScenario scenario, NotificationType type = NotificationType.Info)
    {
        var isMainWindowVisible = IsMainWindowVisible();
        _logger.Log(LogLevel.Debug, "Notify", $"Notify: scenario={scenario} type={type} visible={isMainWindowVisible}");

        // Fallback for unknown/unhandled errors
        if (scenario == NotificationScenario.Generic && type == NotificationType.Error)
        {
            var errorCode = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            var fallbackMessage = $"Unexpected error occurred (Code: {errorCode})";
            _logger.Log(LogLevel.Error, "Notify", $"Generic error [{errorCode}]: {message}");

            if (isMainWindowVisible)
            {
                await ShowBannerAsync(fallbackMessage, NotificationType.Error);
            }
            else
            {
                await ShowTrayToastAsync("Error", fallbackMessage, NotificationType.Error);
            }
            return;
        }

        // Special case: MinimizeToTray always uses Toast
        if (scenario == NotificationScenario.MinimizeToTray)
        {
            await ShowTrayToastAsync("SnowblindMod-Player", message);
            return;
        }

        // Main routing: Banner if window visible, Toast if hidden
        if (isMainWindowVisible)
        {
            // Show banner for all types when window is visible
            await ShowBannerAsync(message, type);
        }
        else
        {
            // Show toast when app is minimized/in tray (pass notification type)
            await ShowTrayToastAsync("Notification", message, type);
        }
    }
}
