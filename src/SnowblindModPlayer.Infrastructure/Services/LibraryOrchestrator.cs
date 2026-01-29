using SnowblindModPlayer.Core.Services;

namespace SnowblindModPlayer.Infrastructure.Services;

/// <summary>
/// Unified library orchestrator for Import/Remove/SetDefault.
/// All library mutations go through here for consistency + event raising.
/// </summary>
public class LibraryOrchestrator : ILibraryOrchestrator
{
    private readonly ILibraryService _libraryService;
    private readonly IImportService _importService;
    private readonly INotificationOrchestrator _notifier;
    private readonly ILoggingService _logger;
    private readonly ILibraryChangeNotifier _changeNotifier;

    public event EventHandler<VideoImportedEventArgs>? VideoImported;
    public event EventHandler<VideoRemovedEventArgs>? VideoRemoved;
    public event EventHandler<DefaultVideoChangedEventArgs>? DefaultVideoChanged;
    public event EventHandler<ImportProgressEventArgs>? ImportProgressChanged;

    public LibraryOrchestrator(
        ILibraryService libraryService,
        IImportService importService,
        INotificationOrchestrator notifier,
        ILoggingService logger,
        ILibraryChangeNotifier changeNotifier)
    {
        _libraryService = libraryService;
        _importService = importService;
        _notifier = notifier;
        _logger = logger;
        _changeNotifier = changeNotifier;

        _importService.ProgressChanged += (s, e) => ImportProgressChanged?.Invoke(this, e);
    }

    public async Task<IReadOnlyList<MediaItem>> ImportVideosAsync(params string[] sourcePaths)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"?? LibraryOrchestrator.ImportVideosAsync: {sourcePaths.Length} file(s)");
            _logger.Log(LogLevel.Info, "Library", $"Import {sourcePaths.Length} file(s)");

            // Delegate to ImportService
            var importedMedia = await _importService.ImportMediaAsync(sourcePaths);

            if (importedMedia.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"? Imported {importedMedia.Count} video(s)");
                _logger.Log(LogLevel.Info, "Library", $"Imported {importedMedia.Count} video(s)");
                
                // Raise events so Tray + UI auto-update
                VideoImported?.Invoke(this, new VideoImportedEventArgs { ImportedVideos = importedMedia });
                _changeNotifier.NotifyVideoImported(importedMedia);

                // Notify user
                await _notifier.NotifyAsync(
                    $"Imported {importedMedia.Count} video(s)",
                    NotificationScenario.ImportSuccess,
                    NotificationType.Success);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("?? No videos imported");
                _logger.Log(LogLevel.Warn, "Library", "No videos imported (invalid or duplicate)");
                
                await _notifier.NotifyAsync(
                    "No videos were imported (invalid or duplicate)",
                    NotificationScenario.ImportError,
                    NotificationType.Warning);
            }

            return importedMedia;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? ImportVideosAsync failed: {ex.Message}");
            _logger.Log(LogLevel.Error, "Library", $"Import failed: {ex.Message}", ex);
            
            await _notifier.NotifyErrorAsync(
                $"Import failed: {ex.Message}",
                ex,
                NotificationScenario.ImportError);

            return new List<MediaItem>();
        }
    }

    public async Task RemoveVideoAsync(string videoId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"??? LibraryOrchestrator.RemoveVideoAsync: {videoId}");
            _logger.Log(LogLevel.Info, "Library", $"Remove video {videoId}");

            // Get video info before deletion (for event + notification)
            var video = await _libraryService.GetMediaByIdAsync(videoId);
            if (video == null)
            {
                await _notifier.NotifyErrorAsync(
                    "Video not found",
                    null,
                    NotificationScenario.RemoveError);
                return;
            }

            var videoName = video.DisplayName;

            // Delete via LibraryService
            await _libraryService.RemoveMediaAsync(videoId);

            System.Diagnostics.Debug.WriteLine($"? Removed: {videoName}");
            _logger.Log(LogLevel.Info, "Library", $"Removed: {videoName}");

            // Raise events so Tray + UI auto-update
            VideoRemoved?.Invoke(this, new VideoRemovedEventArgs 
            { 
                VideoId = videoId, 
                VideoName = videoName 
            });
            _changeNotifier.NotifyVideoRemoved(videoId, videoName);

            // Notify user
            await _notifier.NotifyAsync(
                $"Removed: {videoName}",
                NotificationScenario.RemoveSuccess,
                NotificationType.Success);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? RemoveVideoAsync failed: {ex.Message}");
            _logger.Log(LogLevel.Error, "Library", $"Remove failed: {ex.Message}", ex);
            
            await _notifier.NotifyErrorAsync(
                $"Failed to remove: {ex.Message}",
                ex,
                NotificationScenario.RemoveError);
        }
    }

    public async Task SetDefaultVideoAsync(string videoId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"? LibraryOrchestrator.SetDefaultVideoAsync: {videoId}");
            _logger.Log(LogLevel.Info, "Library", $"Set default video {videoId}");

            // Get video info before setting (for event + notification)
            var video = await _libraryService.GetMediaByIdAsync(videoId);
            if (video == null)
            {
                await _notifier.NotifyErrorAsync(
                    "Video not found",
                    null,
                    NotificationScenario.PlaybackError);
                return;
            }

            var videoName = video.DisplayName;

            // Set as default
            await _libraryService.SetDefaultVideoAsync(videoId);

            System.Diagnostics.Debug.WriteLine($"? Set default: {videoName}");
            _logger.Log(LogLevel.Info, "Library", $"Default set: {videoName}");

            // Raise events so Tray + UI auto-update
            DefaultVideoChanged?.Invoke(this, new DefaultVideoChangedEventArgs 
            { 
                VideoId = videoId, 
                VideoName = videoName 
            });
            _changeNotifier.NotifyDefaultChanged(videoId, videoName);

            // Notify user
            await _notifier.NotifyAsync(
                $"Default set: {videoName}",
                NotificationScenario.DefaultVideoSet,
                NotificationType.Success);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? SetDefaultVideoAsync failed: {ex.Message}");
            _logger.Log(LogLevel.Error, "Library", $"Set default failed: {ex.Message}", ex);
            
            await _notifier.NotifyErrorAsync(
                $"Failed to set default: {ex.Message}",
                ex,
                NotificationScenario.PlaybackError);
        }
    }
}
