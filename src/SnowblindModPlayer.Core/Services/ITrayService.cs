namespace SnowblindModPlayer.Core.Services;

/// <summary>
/// Video item for tray menu (basic data).
/// </summary>
public class VideoItem
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>
/// Abstraction for system tray integration (implemented in App layer).
/// </summary>
public interface ITrayService
{
    /// <summary>Initialize tray (create icon, context menu, hook events).</summary>
    void Initialize(
        Action onShowRequested, 
        Action onExitRequested,
        Func<Task>? onPlayDefaultRequested = null,
        Func<string, Task>? onPlayVideoRequested = null,
        Func<Task>? onStopRequested = null,
        Func<Task<List<VideoItem>>>? getVideosForMenu = null);

    /// <summary>Show a notification balloon.</summary>
    void ShowNotification(string title, string message);

    /// <summary>Update main window visibility (e.g., when minimizing to tray).</summary>
    void SetMainWindowVisible(bool isVisible);

    /// <summary>Cleanup resources on application exit.</summary>
    void Dispose();
}
