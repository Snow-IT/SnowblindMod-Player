using SnowblindModPlayer.Core.Services;

namespace SnowblindModPlayer.Infrastructure.Services;

public class ImportService : IImportService
{
    private readonly ILibraryService _libraryService;
    private readonly ISettingsService _settingsService;
    private readonly IThumbnailQueueService _thumbnailQueueService;
    private readonly IAppDataPathService _appDataPathService;

    public event EventHandler<ImportProgressEventArgs>? ProgressChanged;

    public IReadOnlySet<string> SupportedExtensions { get; } = 
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".webm" };

    public ImportService(
        ILibraryService libraryService,
        ISettingsService settingsService,
        IThumbnailQueueService thumbnailQueueService,
        IAppDataPathService appDataPathService)
    {
        _libraryService = libraryService;
        _settingsService = settingsService;
        _thumbnailQueueService = thumbnailQueueService;
        _appDataPathService = appDataPathService;
    }

    public bool IsValidForImport(string filePath)
    {
        try
        {
            // Check file exists
            if (!File.Exists(filePath))
                return false;

            // Check extension is in whitelist
            var extension = Path.GetExtension(filePath);
            if (!SupportedExtensions.Contains(extension))
                return false;

            // Check file is readable
            using (var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                // File is readable if we can open it
            }

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"IsValidForImport failed for {filePath}: {ex.Message}");
            return false;
        }
    }

    public async Task<IReadOnlyList<MediaItem>> ImportMediaAsync(params string[] sourcePaths)
    {
        var importedMedia = new List<MediaItem>();

        if (sourcePaths == null || sourcePaths.Length == 0)
            return importedMedia.AsReadOnly();

        var total = sourcePaths.Length;
        var processed = 0;
        ProgressChanged?.Invoke(this, new ImportProgressEventArgs
        {
            Total = total,
            Processed = processed,
            Stage = ImportProgressStage.Starting
        });

        var mediaFolder = _settingsService.GetMediaFolder();
        Directory.CreateDirectory(mediaFolder);

        foreach (var sourcePath in sourcePaths)
        {
            try
            {
                ProgressChanged?.Invoke(this, new ImportProgressEventArgs
                {
                    Total = total,
                    Processed = processed,
                    CurrentPath = sourcePath,
                    Stage = ImportProgressStage.Processing
                });

                await Task.Delay(1000);
                // Step 1: Validate
                if (!IsValidForImport(sourcePath))
                {
                    System.Diagnostics.Debug.WriteLine($"Import validation failed for {sourcePath}");
                    processed++;
                    ProgressChanged?.Invoke(this, new ImportProgressEventArgs
                    {
                        Total = total,
                        Processed = processed,
                        CurrentPath = sourcePath,
                        Stage = ImportProgressStage.Skipped,
                        Message = "Invalid file"
                    });
                    continue;
                }

                // Step 2: Check for duplicate (B1)
                var existingBySource = await _libraryService.GetMediaByOriginalPathAsync(sourcePath);
                if (existingBySource != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Duplicate import attempt: {sourcePath} already exists");
                    processed++;
                    ProgressChanged?.Invoke(this, new ImportProgressEventArgs
                    {
                        Total = total,
                        Processed = processed,
                        CurrentPath = sourcePath,
                        Stage = ImportProgressStage.Skipped,
                        Message = "Duplicate"
                    });
                    continue;
                }

                // Step 3: Generate unique filename (C1)
                string fileName = Path.GetFileName(sourcePath);
                string destinationPath = Path.Combine(mediaFolder, fileName);
                destinationPath = GenerateUniqueFilePath(destinationPath);

                // Step 4: Copy file
                File.Copy(sourcePath, destinationPath, overwrite: false);
                System.Diagnostics.Debug.WriteLine($"Copied {sourcePath} to {destinationPath}");

                // Step 5: Create media entry
                var mediaItem = new MediaItem
                {
                    Id = Guid.NewGuid().ToString(),
                    DisplayName = Path.GetFileNameWithoutExtension(fileName),
                    OriginalSourcePath = sourcePath,
                    StoredPath = destinationPath,
                    DateAdded = DateTime.UtcNow,
                    ThumbnailPath = string.Empty // Will be set after thumbnail generation
                };

                // Step 6: Generate thumbnail (via queue for max 1 parallel + timeout/retry)
                try
                {
                    var thumbnailFolder = Path.Combine(mediaFolder, ".thumbnails");
                    Directory.CreateDirectory(thumbnailFolder);
                    var thumbnailPath = Path.Combine(thumbnailFolder, $"{mediaItem.Id}.jpg");
                    
                    System.Diagnostics.Debug.WriteLine($"?? Enqueueing thumbnail for {mediaItem.DisplayName}");
                    System.Diagnostics.Debug.WriteLine($"   Video path: {destinationPath}");
                    System.Diagnostics.Debug.WriteLine($"   Thumbnail path: {thumbnailPath}");
                    
                    await _thumbnailQueueService.EnqueueThumbnailAsync(destinationPath, thumbnailPath);
                    mediaItem.ThumbnailPath = thumbnailPath;
                    
                    System.Diagnostics.Debug.WriteLine($"? Thumbnail enqueued successfully");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"? Thumbnail queueing failed: {ex.Message}");
                    // Continue without thumbnail - import still succeeds
                }

                // Step 7: Persist to database
                await _libraryService.AddMediaAsync(mediaItem);
                importedMedia.Add(mediaItem);
                System.Diagnostics.Debug.WriteLine($"Successfully imported: {mediaItem.DisplayName}");

                processed++;
                ProgressChanged?.Invoke(this, new ImportProgressEventArgs
                {
                    Total = total,
                    Processed = processed,
                    CurrentPath = sourcePath,
                    Stage = ImportProgressStage.Imported
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Import failed for {sourcePath}: {ex.Message}");
                processed++;
                ProgressChanged?.Invoke(this, new ImportProgressEventArgs
                {
                    Total = total,
                    Processed = processed,
                    CurrentPath = sourcePath,
                    Stage = ImportProgressStage.Failed,
                    Message = ex.Message
                });
            }
        }

        ProgressChanged?.Invoke(this, new ImportProgressEventArgs
        {
            Total = total,
            Processed = processed,
            Stage = ImportProgressStage.GeneratingThumbnails,
            Message = "Generating thumbnails..."
        });

        await _thumbnailQueueService.WaitForCompletionAsync();

        ProgressChanged?.Invoke(this, new ImportProgressEventArgs
        {
            Total = total,
            Processed = processed,
            Stage = ImportProgressStage.Completed
        });

        return importedMedia.AsReadOnly();
    }

    /// <summary>
    /// Generates a unique file path by appending (1), (2), etc. if file exists
    /// </summary>
    private string GenerateUniqueFilePath(string filePath)
    {
        if (!File.Exists(filePath))
            return filePath;

        string directory = Path.GetDirectoryName(filePath) ?? "";
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string extension = Path.GetExtension(filePath);

        int counter = 1;
        while (counter < 1000)
        {
            string newPath = Path.Combine(directory, $"{fileName} ({counter}){extension}");
            if (!File.Exists(newPath))
                return newPath;
            counter++;
        }

        // Fallback to GUID if too many collisions
        return Path.Combine(directory, $"{fileName}_{Guid.NewGuid().ToString().Substring(0, 8)}){extension}");
    }
}
