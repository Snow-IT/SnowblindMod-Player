namespace SnowblindModPlayer.Core.Services;

public interface IImportService
{
    /// <summary>
    /// Imports one or more video files into the library.
    /// Performs validation, duplicate checking, file copying, and thumbnail generation.
    /// </summary>
    /// <param name="sourcePaths">Full paths to video files to import</param>
    /// <returns>List of imported MediaItems; empty if all failed</returns>
    Task<IReadOnlyList<MediaItem>> ImportMediaAsync(params string[] sourcePaths);

    /// <summary>
    /// Checks if a file is valid for import (format, existence, readability)
    /// </summary>
    bool IsValidForImport(string filePath);

    /// <summary>
    /// Supported video file extensions (MVP whitelist)
    /// </summary>
    IReadOnlySet<string> SupportedExtensions { get; }
}

/// <summary>
/// Result of a single import attempt
/// </summary>
public class ImportResult
{
    public bool Success { get; set; }
    public MediaItem? ImportedMedia { get; set; }
    public string? ErrorMessage { get; set; }
}
