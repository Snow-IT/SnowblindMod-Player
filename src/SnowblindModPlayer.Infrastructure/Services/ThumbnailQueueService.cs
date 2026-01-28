using SnowblindModPlayer.Core.Services;

namespace SnowblindModPlayer.Infrastructure.Services;

public class ThumbnailQueueService : IThumbnailQueueService
{
    private readonly IThumbnailService _thumbnailService;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private readonly Queue<(string videoPath, string outputPath, TimeSpan? duration)> _queue = new();
    private int _activeCount;
    private Task _processingTask = Task.CompletedTask;

    private const int MaxRetries = 2;
    private const int TimeoutSeconds = 10;

    public ThumbnailQueueService(IThumbnailService thumbnailService)
    {
        _thumbnailService = thumbnailService;
    }

    public Task EnqueueThumbnailAsync(string videoPath, string outputPath, TimeSpan? videoDuration = null)
    {
        lock (_queue)
        {
            _queue.Enqueue((videoPath, outputPath, videoDuration));
            System.Diagnostics.Debug.WriteLine($"?? Thumbnail enqueued: {outputPath} (queue size: {_queue.Count})");
        }

        // Fire processing task if not already running
        _ = ProcessQueueAsync();
        
        return Task.CompletedTask;
    }

    public async Task WaitForCompletionAsync()
    {
        // Wait for queue to be empty AND no active processing
        int checkCount = 0;
        const int maxChecks = 100; // ~10 seconds with 100ms delays

        while (checkCount < maxChecks)
        {
            lock (_queue)
            {
                if (_queue.Count == 0 && _activeCount == 0)
                {
                    System.Diagnostics.Debug.WriteLine("? Thumbnail queue completed");
                    return;
                }
            }

            checkCount++;
            await Task.Delay(100);
        }

        System.Diagnostics.Debug.WriteLine("? Thumbnail queue wait timeout (may still be processing)");
    }

    private async Task ProcessQueueAsync()
    {
        lock (_queue)
        {
            if (_queue.Count == 0)
                return;
        }

        while (true)
        {
            (string videoPath, string outputPath, TimeSpan? duration) item;

            lock (_queue)
            {
                if (_queue.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("? Thumbnail queue processing complete");
                    break;
                }

                item = _queue.Dequeue();
            }

            try
            {
                Interlocked.Increment(ref _activeCount);
                System.Diagnostics.Debug.WriteLine($"? Processing thumbnail: {item.outputPath}");

                // Wait for semaphore (max 1 parallel)
                await _semaphore.WaitAsync();

                try
                {
                    // Execute with timeout and retry
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));

                    for (int attempt = 1; attempt <= MaxRetries; attempt++)
                    {
                        try
                        {
                            await _thumbnailService.GenerateThumbnailAsync(
                                item.videoPath, 
                                item.outputPath, 
                                item.duration,
                                cts.Token);
                            
                            System.Diagnostics.Debug.WriteLine($"? Thumbnail generated: {item.outputPath}");
                            break; // Success, exit retry loop
                        }
                        catch (OperationCanceledException)
                        {
                            if (attempt == MaxRetries)
                            {
                                System.Diagnostics.Debug.WriteLine($"? Thumbnail generation timeout after {MaxRetries} attempts: {item.outputPath}");
                                throw;
                            }

                            System.Diagnostics.Debug.WriteLine($"? Thumbnail generation timeout, retrying ({attempt}/{MaxRetries}): {item.outputPath}");
                            await Task.Delay(500); // Brief delay before retry
                        }
                        catch (Exception ex)
                        {
                            if (attempt == MaxRetries)
                            {
                                System.Diagnostics.Debug.WriteLine($"? Thumbnail generation failed after {MaxRetries} attempts: {item.outputPath} - {ex.Message}");
                                throw;
                            }

                            System.Diagnostics.Debug.WriteLine($"? Thumbnail generation failed, retrying ({attempt}/{MaxRetries}): {item.outputPath} - {ex.Message}");
                            await Task.Delay(500);
                        }
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Thumbnail queue processing failed (import continues): {item.outputPath} - {ex.Message}");
                // Continue processing queue even on failure (import continues without thumbnail)
            }
            finally
            {
                Interlocked.Decrement(ref _activeCount);
            }
        }
    }
}
