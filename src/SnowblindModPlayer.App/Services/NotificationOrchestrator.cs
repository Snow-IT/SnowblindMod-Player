using System.Windows;
using SnowblindModPlayer.Core.Services;

namespace SnowblindModPlayer.Services;

public class NotificationOrchestrator : INotificationOrchestrator
{
    private readonly ITrayService _trayService;

    public NotificationOrchestrator(ITrayService trayService)
    {
        _trayService = trayService;
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
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BANNER-ERROR] {ex.Message}");
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
    public Task ShowTrayToastAsync(string title, string message, int durationMs = 6000)
    {
        try
        {
            _trayService.ShowNotification(title, message);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TOAST-ERROR] {ex.Message}");
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
    /// - If MainWindow visible + non-critical: Show Banner
    /// - If MainWindow hidden: Show Toast (Windows native, visible even if app is tray)
    /// - If Error: Show Dialog if visible, Toast if hidden
    /// - Scenarios like MinimizeToTray always use Toast
    /// </summary>
    public async Task NotifyAsync(string message, NotificationScenario scenario, NotificationType type = NotificationType.Info)
    {
        var isMainWindowVisible = IsMainWindowVisible();

        // Special case: MinimizeToTray always uses Toast
        if (scenario == NotificationScenario.MinimizeToTray)
        {
            await ShowTrayToastAsync("SnowblindMod-Player", message);
            return;
        }

        // Error scenarios: Dialog if visible, Toast if hidden
        if (type == NotificationType.Error)
        {
            if (isMainWindowVisible)
            {
                await ShowErrorAsync("Error", message);
            }
            else
            {
                // Toast for errors when app is minimized/tray (e.g., playback error, missing file)
                await ShowTrayToastAsync("Error", message);
            }
            return;
        }

        // Playback-related errors should always show Toast if app is not visible
        // (ensures user sees "file missing" even if app is in tray)
        if (scenario == NotificationScenario.PlaybackError)
        {
            if (isMainWindowVisible)
            {
                await ShowBannerAsync(message, type, 5000);
            }
            else
            {
                await ShowTrayToastAsync("SnowblindMod-Player", message);
            }
            return;
        }

        // Default: Banner if visible, Toast if hidden
        if (isMainWindowVisible)
        {
            await ShowBannerAsync(message, type, 5000);
        }
        else
        {
            await ShowTrayToastAsync("SnowblindMod-Player", message);
        }
    }
}
