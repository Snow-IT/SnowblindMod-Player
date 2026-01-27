using FFMpegCore;
using FFMpegCore.Enums;
using SnowblindModPlayer.Core.Services;

namespace SnowblindModPlayer.Infrastructure.Services;

/// <summary>
/// FFmpeg-based thumbnail generator.
/// Replaces LibVLC snapshot approach for better reliability and decoupling from playback engine.
/// </summary>
public class ThumbnailServiceFFmpeg : IThumbnailService
{
    private const int ThumbnailWidth = 320;
    private const int ThumbnailHeight = 180;
    private const double SeekPercentage = 0.05; // 5% into video
    private readonly SemaphoreSlim _semaphore = new(1, 1); // Max 1 concurrent thumbnail

    public async Task<string> GenerateThumbnailAsync(string videoPath, string outputPath, TimeSpan? videoDuration = null)
    {
        await _semaphore.WaitAsync();
        try
        {
            // Ensure output directory exists
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Determine seek time: use provided duration or probe video
            TimeSpan seekTime;
            if (videoDuration.HasValue && videoDuration.Value.TotalSeconds > 0)
            {
                seekTime = TimeSpan.FromSeconds(videoDuration.Value.TotalSeconds * SeekPercentage);
            }
            else
            {
                // Probe video for duration
                var mediaInfo = await FFProbe.AnalyseAsync(videoPath);
                var duration = mediaInfo.Duration;
                seekTime = duration.TotalSeconds > 0 
                    ? TimeSpan.FromSeconds(duration.TotalSeconds * SeekPercentage)
                    : TimeSpan.FromSeconds(1); // Fallback to 1s if duration unknown
            }

            // Extract frame at seek position
            await FFMpeg.SnapshotAsync(
                input: videoPath,
                output: outputPath,
                captureTime: seekTime,
                size: new System.Drawing.Size(ThumbnailWidth, ThumbnailHeight)
            );

            return outputPath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FFmpeg thumbnail generation failed: {ex.Message}");
            
            // Fallback: Try frame at 1s
            try
            {
                await FFMpeg.SnapshotAsync(
                    input: videoPath,
                    output: outputPath,
                    captureTime: TimeSpan.FromSeconds(1),
                    size: new System.Drawing.Size(ThumbnailWidth, ThumbnailHeight)
                );
                return outputPath;
            }
            catch
            {
                // Final fallback: Try first frame
                try
                {
                    await FFMpeg.SnapshotAsync(
                        input: videoPath,
                        output: outputPath,
                        captureTime: TimeSpan.Zero,
                        size: new System.Drawing.Size(ThumbnailWidth, ThumbnailHeight)
                    );
                    return outputPath;
                }
                catch (Exception finalEx)
                {
                    System.Diagnostics.Debug.WriteLine($"All FFmpeg fallbacks failed: {finalEx.Message}");
                    throw new InvalidOperationException($"Failed to generate thumbnail: {finalEx.Message}", finalEx);
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _semaphore?.Dispose();
    }
}
