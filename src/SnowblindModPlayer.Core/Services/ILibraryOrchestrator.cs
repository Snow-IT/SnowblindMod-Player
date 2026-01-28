namespace SnowblindModPlayer.Core.Services;

/// <summary>
/// Unified orchestrator for library mutations (Import/Remove/SetDefault).
/// Single entry point for all library operations.
/// Raises events so UI/Tray auto-update on changes.
/// </summary>
public interface ILibraryOrchestrator
{
    // Events for library changes (UI + Tray subscribe to these)
    event EventHandler<VideoImportedEventArgs>? VideoImported;
    event EventHandler<VideoRemovedEventArgs>? VideoRemoved;
    event EventHandler<DefaultVideoChangedEventArgs>? DefaultVideoChanged;

    // Operations
    Task<IReadOnlyList<MediaItem>> ImportVideosAsync(params string[] sourcePaths);
    Task RemoveVideoAsync(string videoId);
    Task SetDefaultVideoAsync(string videoId);
}

public class VideoImportedEventArgs : EventArgs
{
    public IReadOnlyList<MediaItem> ImportedVideos { get; set; } = new List<MediaItem>();
}

public class VideoRemovedEventArgs : EventArgs
{
    public string VideoId { get; set; } = string.Empty;
    public string VideoName { get; set; } = string.Empty;
}

public class DefaultVideoChangedEventArgs : EventArgs
{
    public string? VideoId { get; set; }
    public string VideoName { get; set; } = string.Empty;
}
