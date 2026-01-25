namespace SnowblindModPlayer.Core.Services;

public interface IThumbnailQueueService
{
    /// <summary>
    /// Enqueue thumbnail generation with max 1 parallel task, timeout, and retry logic.
    /// </summary>
    Task EnqueueThumbnailAsync(string videoPath, string outputPath, TimeSpan? videoDuration = null);

    /// <summary>
    /// Wait for all pending tasks to complete.
    /// </summary>
    Task WaitForCompletionAsync();
}
