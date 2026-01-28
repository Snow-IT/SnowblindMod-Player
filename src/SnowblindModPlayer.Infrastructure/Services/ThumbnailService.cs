using LibVLCSharp.Shared;
using SnowblindModPlayer.Core.Services;

namespace SnowblindModPlayer.Infrastructure.Services;

public class ThumbnailService : IThumbnailService
{
    private const int ThumbnailWidth = 320;
    private const double AspectRatio = 16.0 / 9.0;
    private const int ThumbnailHeight = (int)(ThumbnailWidth / AspectRatio); // 180px

    private readonly LibVLC? _libVLC;

    public ThumbnailService()
    {
        try
        {
            LibVLCSharp.Shared.Core.Initialize();
            _libVLC = new LibVLC();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LibVLC initialization failed: {ex.Message}");
            _libVLC = null;
        }
    }

    public async Task<string> GenerateThumbnailAsync(
        string videoPath, 
        string outputPath, 
        TimeSpan? videoDuration = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"?? GenerateThumbnailAsync: {Path.GetFileName(videoPath)} ? {Path.GetFileName(outputPath)}");
            
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(videoPath) || !File.Exists(videoPath))
            {
                throw new FileNotFoundException($"Video file not found: {videoPath}");
            }

            // Ensure output directory exists
            string outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // Try VLC snapshot, fallback to placeholder
            if (_libVLC != null)
            {
                System.Diagnostics.Debug.WriteLine($"   ?? LibVLC available, attempting VLC snapshot...");
                if (await TryGenerateVLCSnapshotAsync(videoPath, outputPath, videoDuration, cancellationToken))
                {
                    System.Diagnostics.Debug.WriteLine($"? VLC snapshot successful: {outputPath}");
                    return outputPath;
                }
                System.Diagnostics.Debug.WriteLine($"? VLC snapshot failed, using placeholder");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"??  LibVLC not available, using placeholder");
            }

            // Fallback to placeholder
            await CreatePlaceholderThumbnailAsync(outputPath);
            System.Diagnostics.Debug.WriteLine($"?? Placeholder thumbnail created: {outputPath}");
            return outputPath;
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine($"?? Thumbnail generation cancelled: {outputPath}");
            throw;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Thumbnail generation error: {ex.Message}, creating placeholder...");
            
            try
            {
                // Final fallback to placeholder
                await CreatePlaceholderThumbnailAsync(outputPath);
                System.Diagnostics.Debug.WriteLine($"?? Fallback placeholder created");
            }
            catch (Exception placeholderEx)
            {
                System.Diagnostics.Debug.WriteLine($"? Placeholder creation also failed: {placeholderEx.Message}");
            }

            return outputPath;
        }
    }

    private async Task<bool> TryGenerateVLCSnapshotAsync(
        string videoPath, 
        string outputPath, 
        TimeSpan? videoDuration,
        CancellationToken cancellationToken)
    {
        if (_libVLC == null)
            return false;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var media = new Media(_libVLC, videoPath, FromType.FromPath);
            using var mediaPlayer = new MediaPlayer(_libVLC);

            mediaPlayer.Media = media;

            // Parse media to get duration
            var parseTask = media.Parse(MediaParseOptions.ParseLocal);
            var parseStatus = parseTask.GetAwaiter().GetResult();

            if (parseStatus != MediaParsedStatus.Done)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to parse media: {videoPath}");
                return false;
            }

            long durationMs = media.Duration;
            if (durationMs <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"Invalid duration for media: {videoPath}");
                return false;
            }

            // Calculate 5% position with fallback
            long snapshotTimeMs = (long)(durationMs * 0.05); // 5%
            if (snapshotTimeMs < 1000) // Less than 1 second
                snapshotTimeMs = 1000; // Fallback to 1s

            System.Diagnostics.Debug.WriteLine($"Extracting snapshot at {snapshotTimeMs}ms (5% of {durationMs}ms)");

            // Take snapshot
            mediaPlayer.Play();

            // Wait for playback to start and reach snapshot position
            await Task.Delay(500, cancellationToken); // Let playback initialize
            mediaPlayer.Time = snapshotTimeMs;
            await Task.Delay(500, cancellationToken); // Let frame decode

            cancellationToken.ThrowIfCancellationRequested();

            // Save snapshot
            bool success = mediaPlayer.TakeSnapshot(0, outputPath, ThumbnailWidth, ThumbnailHeight);

            if (!success)
            {
                System.Diagnostics.Debug.WriteLine($"VLC snapshot failed for: {videoPath}");
                return false;
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine($"VLC snapshot generation cancelled: {videoPath}");
            throw;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"VLC snapshot generation exception: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Creates a simple gray placeholder thumbnail (320x180) as JPG.
    /// </summary>
    private async Task CreatePlaceholderThumbnailAsync(string outputPath)
    {
        await Task.Run(() =>
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            // Minimal 1x1 JPEG (JFIF) bytes
            var jpg = new byte[]
            {
                0xFF,0xD8,0xFF,0xE0,0x00,0x10,0x4A,0x46,0x49,0x46,0x00,0x01,0x01,0x01,0x00,0x48,0x00,0x48,0x00,0x00,
                0xFF,0xDB,0x00,0x43,0x00,
                0x08,0x06,0x06,0x07,0x06,0x05,0x08,0x07,0x07,0x07,0x09,0x09,0x08,0x0A,0x0C,0x14,0x0D,0x0C,0x0B,0x0B,0x0C,0x19,0x12,0x13,0x0F,0x14,0x1D,0x1A,0x1F,0x1E,0x1D,0x1A,0x1C,0x1C,0x20,0x24,0x2E,0x27,0x20,0x22,0x2C,0x23,0x1C,0x1C,0x28,0x37,0x29,0x2C,0x30,0x31,0x34,0x34,0x34,0x1F,0x27,0x39,0x3D,0x38,0x32,0x3C,0x2E,0x33,0x34,0x32,
                0xFF,0xC0,0x00,0x11,0x08,0x00,0x01,0x00,0x01,0x03,0x01,0x11,0x00,0x02,0x11,0x01,0x03,0x11,0x01,
                0xFF,0xC4,0x00,0x14,0x00,0x01,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
                0xFF,0xC4,0x00,0x14,0x10,0x01,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
                0xFF,0xDA,0x00,0x0C,0x03,0x01,0x00,0x02,0x11,0x03,0x11,0x00,0x3F,0x00,
                0x00,
                0xFF,0xD9
            };

            File.WriteAllBytes(outputPath, jpg);
        });
    }
}
