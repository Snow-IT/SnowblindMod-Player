namespace SnowblindModPlayer.Core.Services;

public interface ILibraryChangeNotifier
{
    event EventHandler<VideoImportedEventArgs>? VideoImported;
    event EventHandler<VideoRemovedEventArgs>? VideoRemoved;
    event EventHandler<DefaultVideoChangedEventArgs>? DefaultVideoChanged;

    void NotifyVideoImported(IReadOnlyList<MediaItem> imported);
    void NotifyVideoRemoved(string videoId, string videoName);
    void NotifyDefaultChanged(string videoId, string videoName);
}
