using FFMpegCore;
using FFMpegCore.Enums;
using SnowblindModPlayer.Core.Services;

namespace SnowblindModPlayer.Infrastructure.Services;

/// <summary>
/// [DEPRECATED - NOT USED]
/// FFmpeg-based thumbnail generator.
/// Requires FFmpeg/ffprobe binaries in system PATH or app directory.
/// 
/// CURRENT STATE: LibVLC-based ThumbnailService.cs is used instead.
/// This implementation remains as reference for future migration if FFmpeg becomes required.
/// 
/// To use this instead of LibVLC:
/// 1. Ensure FFmpeg is installed on system or bundled with app
/// 2. Update ServiceCollectionExtensions.cs: services.AddSingleton<IThumbnailService, ThumbnailServiceFFmpeg>();
/// </summary>
public class ThumbnailServiceFFmpeg : IThumbnailService
{
    private const int ThumbnailWidth = 320;
    private const int ThumbnailHeight = 180;
    private const double SeekPercentage = 0.05; // 5% into video
    private readonly SemaphoreSlim _semaphore = new(1, 1); // Max 1 concurrent thumbnail

    public async Task<string> GenerateThumbnailAsync(
        string videoPath, 
        string outputPath, 
        TimeSpan? videoDuration = null,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Validate input
            if (!File.Exists(videoPath))
            {
                throw new FileNotFoundException($"Video file not found: {videoPath}");
            }

            // Ensure output directory exists
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                System.Diagnostics.Debug.WriteLine($"?? Created thumbnail directory: {directory}");
            }

            // Determine seek time: use provided duration or probe video
            TimeSpan seekTime;
            if (videoDuration.HasValue && videoDuration.Value.TotalSeconds > 0)
            {
                seekTime = TimeSpan.FromSeconds(videoDuration.Value.TotalSeconds * SeekPercentage);
                System.Diagnostics.Debug.WriteLine($"?? Using provided duration: {videoDuration.Value.TotalSeconds}s, seeking to {seekTime.TotalSeconds}s");
            }
            else
            {
                // Probe video for duration
                System.Diagnostics.Debug.WriteLine($"?? Probing video: {videoPath}");
                var mediaInfo = await FFProbe.AnalyseAsync(videoPath, cancellationToken: cancellationToken);
                var duration = mediaInfo.Duration;
                
                System.Diagnostics.Debug.WriteLine($"?? Video duration: {duration.TotalSeconds}s");
                
                seekTime = duration.TotalSeconds > 0 
                    ? TimeSpan.FromSeconds(duration.TotalSeconds * SeekPercentage)
                    : TimeSpan.FromSeconds(1); // Fallback to 1s if duration unknown
                    
                System.Diagnostics.Debug.WriteLine($"?? Calculated seek time: {seekTime.TotalSeconds}s");
            }

            // Extract frame at seek position
            System.Diagnostics.Debug.WriteLine($"?? Extracting thumbnail at {seekTime.TotalSeconds}s -> {outputPath}");
            
            await FFMpeg.SnapshotAsync(
                input: videoPath,
                output: outputPath,
                captureTime: seekTime,
                size: new System.Drawing.Size(ThumbnailWidth, ThumbnailHeight),
                cancellationToken: cancellationToken
            );

            System.Diagnostics.Debug.WriteLine($"? Thumbnail saved: {outputPath}");
            return outputPath;
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine($"? Thumbnail generation cancelled: {outputPath}");
            throw;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? FFmpeg snapshot at {(videoDuration?.TotalSeconds ?? 0.05 * 100)}% failed: {ex.Message}");
            
            // Fallback 1: Try frame at 1s
            try
            {
                System.Diagnostics.Debug.WriteLine($"?? Fallback 1: Trying frame at 1s");
                await FFMpeg.SnapshotAsync(
                    input: videoPath,
                    output: outputPath,
                    captureTime: TimeSpan.FromSeconds(1),
                    size: new System.Drawing.Size(ThumbnailWidth, ThumbnailHeight),
                    cancellationToken: cancellationToken
                );
                System.Diagnostics.Debug.WriteLine($"? Thumbnail saved (fallback 1s): {outputPath}");
                return outputPath;
            }
            catch (Exception ex1)
            {
                System.Diagnostics.Debug.WriteLine($"? Fallback 1 failed: {ex1.Message}");
                
                // Fallback 2: Try first frame
                try
                {
                    System.Diagnostics.Debug.WriteLine($"?? Fallback 2: Trying first frame (0s)");
                    await FFMpeg.SnapshotAsync(
                        input: videoPath,
                        output: outputPath,
                        captureTime: TimeSpan.Zero,
                        size: new System.Drawing.Size(ThumbnailWidth, ThumbnailHeight),
                        cancellationToken: cancellationToken
                    );
                    System.Diagnostics.Debug.WriteLine($"? Thumbnail saved (fallback 0s): {outputPath}");
                    return outputPath;
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"? All FFmpeg fallbacks failed: {ex2.Message}");
                    throw new InvalidOperationException(
                        $"Failed to generate thumbnail for {videoPath}: {ex2.Message}", 
                        ex2);
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
