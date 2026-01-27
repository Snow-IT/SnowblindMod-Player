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

    public Task ShowBannerAsync(string message, NotificationType type = NotificationType.Info, int durationMs = 5000)
    {
        // Placeholder: no visual banner yet; log for now
        System.Diagnostics.Debug.WriteLine($"[BANNER-{type}] {message}");
        return Task.CompletedTask;
    }

    public Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return Task.FromResult(result == MessageBoxResult.Yes);
    }

    public Task ShowErrorAsync(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        return Task.CompletedTask;
    }

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

    public async Task NotifyAsync(string message, NotificationScenario scenario, NotificationType type = NotificationType.Info)
    {
        var mainWindow = Application.Current?.MainWindow as MainWindow;
        var isVisible = mainWindow != null && mainWindow.IsVisible && mainWindow.Visibility == Visibility.Visible && mainWindow.ShowInTaskbar;

        if (scenario == NotificationScenario.MinimizeToTray)
        {
            await ShowTrayToastAsync("Snowblind-Mod Player", message);
            return;
        }

        if (type == NotificationType.Error)
        {
            if (isVisible)
            {
                await ShowErrorAsync("Error", message);
            }
            else
            {
                await ShowTrayToastAsync("Error", message);
            }
            return;
        }

        if (isVisible && mainWindow != null)
        {
            mainWindow.ShowBanner(message, type, 6000);
        }
        else
        {
            await ShowTrayToastAsync("Snowblind-Mod Player", message);
        }
    }
}
