using SnowblindModPlayer.Core.Services;

namespace SnowblindModPlayer.Infrastructure.Services;

public class ThumbnailService : IThumbnailService
{
    public Task<string> GenerateThumbnailAsync(string videoPath, string outputPath, TimeSpan? videoDuration = null)
    {
        // TODO: Implement VLC snapshot at 5% duration with fallback
        return Task.FromResult(outputPath);
    }
}
