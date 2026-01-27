namespace SnowblindModPlayer.Core.Services;

public enum NotificationType
{
    Info,
    Warning,
    Error,
    Success
}

public enum NotificationScenario
{
    ImportSuccess,
    ImportError,
    RemoveSuccess,
    RemoveError,
    PlaybackError,
    DefaultVideoSet,
    AutoplayStarted,
    MinimizeToTray,
    SettingsSaved,
    Generic
}

public interface INotificationOrchestrator
{
    Task ShowBannerAsync(string message, NotificationType type = NotificationType.Info, int durationMs = 5000);
    Task<bool> ShowConfirmationAsync(string title, string message);
    Task ShowErrorAsync(string title, string message);
    Task ShowTrayToastAsync(string title, string message, int durationMs = 6000);
    Task NotifyAsync(string message, NotificationScenario scenario, NotificationType type = NotificationType.Info);
}
