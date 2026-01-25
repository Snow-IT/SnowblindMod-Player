namespace SnowblindModPlayer.Core.Services;

public interface ILibraryService
{
    Task<IReadOnlyList<MediaItem>> GetAllMediaAsync();
    Task<MediaItem?> GetMediaByIdAsync(string id);
    Task AddMediaAsync(MediaItem media);
    Task RemoveMediaAsync(string id);
    Task SetDefaultVideoAsync(string? videoId);
    Task<MediaItem?> GetDefaultVideoAsync();
    Task CleanupOrphanedEntriesAsync();
}

public class MediaItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DisplayName { get; set; } = string.Empty;
    public string OriginalSourcePath { get; set; } = string.Empty;
    public string StoredPath { get; set; } = string.Empty;
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;
    public string ThumbnailPath { get; set; } = string.Empty;
}
