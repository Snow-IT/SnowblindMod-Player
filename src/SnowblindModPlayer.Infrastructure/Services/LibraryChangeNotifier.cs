using SnowblindModPlayer.Core.Services;

namespace SnowblindModPlayer.Infrastructure.Services;

public class LibraryChangeNotifier : ILibraryChangeNotifier
{
    public event EventHandler<VideoImportedEventArgs>? VideoImported;
    public event EventHandler<VideoRemovedEventArgs>? VideoRemoved;
    public event EventHandler<DefaultVideoChangedEventArgs>? DefaultVideoChanged;

    public void NotifyVideoImported(IReadOnlyList<MediaItem> imported)
        => VideoImported?.Invoke(this, new VideoImportedEventArgs { ImportedVideos = imported });

    public void NotifyVideoRemoved(string videoId, string videoName)
        => VideoRemoved?.Invoke(this, new VideoRemovedEventArgs { VideoId = videoId, VideoName = videoName });

    public void NotifyDefaultChanged(string videoId, string videoName)
        => DefaultVideoChanged?.Invoke(this, new DefaultVideoChangedEventArgs { VideoId = videoId, VideoName = videoName });
}
