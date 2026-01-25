using SnowblindModPlayer.Core.Services;

namespace SnowblindModPlayer.Infrastructure.Services;

public class ImportService : IImportService
{
    private readonly ILibraryService _libraryService;
    private readonly ISettingsService _settingsService;
    private readonly IThumbnailService _thumbnailService;
    private readonly IAppDataPathService _appDataPathService;

    public IReadOnlySet<string> SupportedExtensions { get; } = 
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".webm" };

    public ImportService(
        ILibraryService libraryService,
        ISettingsService settingsService,
        IThumbnailService thumbnailService,
        IAppDataPathService appDataPathService)
    {
        _libraryService = libraryService;
        _settingsService = settingsService;
        _thumbnailService = thumbnailService;
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

        var mediaFolder = _settingsService.GetMediaFolder();
        Directory.CreateDirectory(mediaFolder);

        foreach (var sourcePath in sourcePaths)
        {
            try
            {
                // Step 1: Validate
                if (!IsValidForImport(sourcePath))
                {
                    System.Diagnostics.Debug.WriteLine($"Import validation failed for {sourcePath}");
                    continue;
                }

                // Step 2: Check for duplicate (B1)
                var existingBySource = await _libraryService.GetMediaByOriginalPathAsync(sourcePath);
                if (existingBySource != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Duplicate import attempt: {sourcePath} already exists");
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

                // Step 6: Generate thumbnail (fire and forget for now, enhanced queue in future)
                try
                {
                    var thumbnailFolder = Path.Combine(mediaFolder, ".thumbnails");
                    Directory.CreateDirectory(thumbnailFolder);
                    var thumbnailPath = Path.Combine(thumbnailFolder, $"{mediaItem.Id}.jpg");
                    
                    await _thumbnailService.GenerateThumbnailAsync(destinationPath, thumbnailPath);
                    mediaItem.ThumbnailPath = thumbnailPath;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Thumbnail generation failed: {ex.Message}");
                    // Continue without thumbnail - import still succeeds
                }

                // Step 7: Persist to database
                await _libraryService.AddMediaAsync(mediaItem);
                importedMedia.Add(mediaItem);
                System.Diagnostics.Debug.WriteLine($"Successfully imported: {mediaItem.DisplayName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Import failed for {sourcePath}: {ex.Message}");
            }
        }

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
