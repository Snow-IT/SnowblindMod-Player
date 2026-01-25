using SnowblindModPlayer.Core.Services;

namespace SnowblindModPlayer.Infrastructure.Services;

public class LibraryService : ILibraryService
{
    private readonly List<MediaItem> _mediaItems = new();

    public Task<IReadOnlyList<MediaItem>> GetAllMediaAsync()
    {
        return Task.FromResult<IReadOnlyList<MediaItem>>(_mediaItems.AsReadOnly());
    }

    public Task<MediaItem?> GetMediaByIdAsync(string id)
    {
        var item = _mediaItems.FirstOrDefault(x => x.Id == id);
        return Task.FromResult(item);
    }

    public Task AddMediaAsync(MediaItem media)
    {
        _mediaItems.Add(media);
        return Task.CompletedTask;
    }

    public Task RemoveMediaAsync(string id)
    {
        _mediaItems.RemoveAll(x => x.Id == id);
        return Task.CompletedTask;
    }

    public Task SetDefaultVideoAsync(string? videoId)
    {
        // TODO: Store in settings
        return Task.CompletedTask;
    }

    public Task<MediaItem?> GetDefaultVideoAsync()
    {
        // TODO: Retrieve from settings
        return Task.FromResult<MediaItem?>(null);
    }

    public Task CleanupOrphanedEntriesAsync()
    {
        // E1: Remove entries with non-existent storedPath
        var toRemove = _mediaItems.Where(x => !File.Exists(x.StoredPath)).ToList();
        foreach (var item in toRemove)
        {
            _mediaItems.Remove(item);
        }
        return Task.CompletedTask;
    }
}
