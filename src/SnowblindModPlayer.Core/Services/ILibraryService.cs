using SnowblindModPlayer.Core.Services;

namespace SnowblindModPlayer.Core.Services;

public class MediaItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DisplayName { get; set; } = string.Empty;
    public string OriginalSourcePath { get; set; } = string.Empty;
    public string StoredPath { get; set; } = string.Empty;
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;
    public string ThumbnailPath { get; set; } = string.Empty;
}

public interface ILibraryService
{
    /// <summary>
    /// Get all media items ordered by DateAdded (newest first).
    /// </summary>
    Task<IReadOnlyList<MediaItem>> GetAllMediaAsync();

    /// <summary>
    /// Get a specific media item by ID.
    /// </summary>
    Task<MediaItem?> GetMediaByIdAsync(string id);

    /// <summary>
    /// Get media item by original source path (for duplicate detection).
    /// </summary>
    Task<MediaItem?> GetMediaByOriginalPathAsync(string originalSourcePath);

    /// <summary>
    /// Add a new media item to the library.
    /// </summary>
    Task AddMediaAsync(MediaItem media);

    /// <summary>
    /// Remove a media item and its files.
    /// </summary>
    Task RemoveMediaAsync(string id);

    /// <summary>
    /// Set the default video for autoplay.
    /// </summary>
    Task SetDefaultVideoAsync(string? videoId);

    /// <summary>
    /// Get the default video for autoplay.
    /// </summary>
    Task<MediaItem?> GetDefaultVideoAsync();

    /// <summary>
    /// Remove orphaned database entries (E1 cleanup).
    /// </summary>
    Task CleanupOrphanedEntriesAsync();
}
