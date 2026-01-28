namespace SnowblindModPlayer.Core.Services;

public interface IThumbnailService
{
    Task<string> GenerateThumbnailAsync(
        string videoPath, 
        string outputPath, 
        TimeSpan? videoDuration = null,
        CancellationToken cancellationToken = default);
}
