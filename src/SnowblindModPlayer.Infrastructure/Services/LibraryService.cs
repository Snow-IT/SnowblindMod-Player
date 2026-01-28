using Microsoft.Data.Sqlite;
using SnowblindModPlayer.Core.Services;
using SnowblindModPlayer.Infrastructure.Data;

namespace SnowblindModPlayer.Infrastructure.Services;

public class LibraryService : ILibraryService
{
    private readonly LibraryDbContext _dbContext;
    private readonly ISettingsService _settingsService;
    private const string DefaultVideoIdKey = "DefaultVideoId";

    public LibraryService(LibraryDbContext dbContext, ISettingsService settingsService)
    {
        _dbContext = dbContext;
        _settingsService = settingsService;
    }

    public async Task<IReadOnlyList<MediaItem>> GetAllMediaAsync()
    {
        var mediaItems = new List<MediaItem>();

        try
        {
            using (var connection = _dbContext.GetConnection())
            {
                await connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT Id, DisplayName, OriginalSourcePath, StoredPath, DateAdded, ThumbnailPath
                        FROM Media
                        ORDER BY DateAdded DESC
                    ";

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            mediaItems.Add(new MediaItem
                            {
                                Id = reader.GetString(0),
                                DisplayName = reader.GetString(1),
                                OriginalSourcePath = reader.GetString(2),
                                StoredPath = reader.GetString(3),
                                DateAdded = DateTime.Parse(reader.GetString(4)),
                                ThumbnailPath = reader.IsDBNull(5) ? string.Empty : reader.GetString(5)
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetAllMediaAsync failed: {ex.Message}");
        }

        return mediaItems.AsReadOnly();
    }

    public async Task<MediaItem?> GetMediaByIdAsync(string id)
    {
        try
        {
            using (var connection = _dbContext.GetConnection())
            {
                await connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT Id, DisplayName, OriginalSourcePath, StoredPath, DateAdded, ThumbnailPath
                        FROM Media
                        WHERE Id = @id
                    ";
                    command.Parameters.AddWithValue("@id", id);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new MediaItem
                            {
                                Id = reader.GetString(0),
                                DisplayName = reader.GetString(1),
                                OriginalSourcePath = reader.GetString(2),
                                StoredPath = reader.GetString(3),
                                DateAdded = DateTime.Parse(reader.GetString(4)),
                                ThumbnailPath = reader.IsDBNull(5) ? string.Empty : reader.GetString(5)
                            };
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetMediaByIdAsync failed: {ex.Message}");
        }

        return null;
    }

    public async Task AddMediaAsync(MediaItem media)
    {
        try
        {
            using (var connection = _dbContext.GetConnection())
            {
                await connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        INSERT INTO Media (Id, DisplayName, OriginalSourcePath, StoredPath, DateAdded, ThumbnailPath)
                        VALUES (@id, @displayName, @originalSourcePath, @storedPath, @dateAdded, @thumbnailPath)
                    ";
                    command.Parameters.AddWithValue("@id", media.Id);
                    command.Parameters.AddWithValue("@displayName", media.DisplayName);
                    command.Parameters.AddWithValue("@originalSourcePath", media.OriginalSourcePath);
                    command.Parameters.AddWithValue("@storedPath", media.StoredPath);
                    command.Parameters.AddWithValue("@dateAdded", media.DateAdded.ToString("O"));
                    command.Parameters.AddWithValue("@thumbnailPath", media.ThumbnailPath ?? string.Empty);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AddMediaAsync failed: {ex.Message}");
            throw;
        }
    }

    public async Task RemoveMediaAsync(string id)
    {
        try
        {
            using (var connection = _dbContext.GetConnection())
            {
                await connection.OpenAsync();

                // First get the media to find files to delete
                var media = await GetMediaByIdAsync(id);
                if (media != null)
                {
                    // Delete video file (skip silently if already gone)
                    try
                    {
                        if (!string.IsNullOrEmpty(media.StoredPath) && File.Exists(media.StoredPath))
                        {
                            File.Delete(media.StoredPath);
                            System.Diagnostics.Debug.WriteLine($"? Deleted video file: {media.StoredPath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log but don't throw - file may already be deleted manually
                        System.Diagnostics.Debug.WriteLine($"? Could not delete video file: {ex.Message}");
                    }

                    // Delete thumbnail (skip silently if already gone)
                    try
                    {
                        if (!string.IsNullOrEmpty(media.ThumbnailPath) && File.Exists(media.ThumbnailPath))
                        {
                            File.Delete(media.ThumbnailPath);
                            System.Diagnostics.Debug.WriteLine($"? Deleted thumbnail: {media.ThumbnailPath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log but don't throw - thumbnail may already be deleted or was never created
                        System.Diagnostics.Debug.WriteLine($"? Could not delete thumbnail: {ex.Message}");
                    }
                }

                // Delete from database (this will always succeed if ID exists)
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM Media WHERE Id = @id";
                    command.Parameters.AddWithValue("@id", id);
                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    System.Diagnostics.Debug.WriteLine($"? Deleted {rowsAffected} database record(s)");
                }

                // Reset default video if it was this video
                var defaultVideoId = _settingsService.Get(DefaultVideoIdKey, string.Empty);
                if (defaultVideoId == id)
                {
                    _settingsService.Set(DefaultVideoIdKey, string.Empty);
                    System.Diagnostics.Debug.WriteLine($"? Reset default video (was: {id})");
                    await _settingsService.SaveAsync();
                }
            }

            System.Diagnostics.Debug.WriteLine($"? RemoveMediaAsync completed successfully for ID: {id}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? RemoveMediaAsync failed: {ex.Message}");
            throw;
        }
    }

    public async Task SetDefaultVideoAsync(string? videoId)
    {
        if (videoId != null)
        {
            var media = await GetMediaByIdAsync(videoId);
            if (media == null)
            {
                throw new ArgumentException($"Media with ID '{videoId}' not found");
            }
        }

        _settingsService.Set(DefaultVideoIdKey, videoId ?? string.Empty);
        await _settingsService.SaveAsync();
    }

    public async Task<MediaItem?> GetDefaultVideoAsync()
    {
        var defaultVideoId = _settingsService.Get(DefaultVideoIdKey, string.Empty);
        if (string.IsNullOrEmpty(defaultVideoId))
            return null;

        return await GetMediaByIdAsync(defaultVideoId);
    }

    public async Task CleanupOrphanedEntriesAsync()
    {
        try
        {
            using (var connection = _dbContext.GetConnection())
            {
                await connection.OpenAsync();

                // Find all media entries where StoredPath doesn't exist
                var orphanedIds = new List<string>();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT Id, StoredPath FROM Media";

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var id = reader.GetString(0);
                            var storedPath = reader.GetString(1);

                            if (!File.Exists(storedPath))
                            {
                                orphanedIds.Add(id);
                            }
                        }
                    }
                }

                // Delete orphaned entries
                foreach (var id in orphanedIds)
                {
                    await RemoveMediaAsync(id);
                    System.Diagnostics.Debug.WriteLine($"Cleaned up orphaned media entry: {id}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CleanupOrphanedEntriesAsync failed: {ex.Message}");
        }
    }

    public async Task<MediaItem?> GetMediaByOriginalPathAsync(string originalPath)
    {
        try
        {
            using (var connection = _dbContext.GetConnection())
            {
                await connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT Id, DisplayName, OriginalSourcePath, StoredPath, DateAdded, ThumbnailPath
                        FROM Media
                        WHERE OriginalSourcePath = @originalPath
                    ";
                    command.Parameters.AddWithValue("@originalPath", originalPath);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new MediaItem
                            {
                                Id = reader.GetString(0),
                                DisplayName = reader.GetString(1),
                                OriginalSourcePath = reader.GetString(2),
                                StoredPath = reader.GetString(3),
                                DateAdded = DateTime.Parse(reader.GetString(4)),
                                ThumbnailPath = reader.IsDBNull(5) ? string.Empty : reader.GetString(5)
                            };
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetMediaByOriginalPathAsync failed: {ex.Message}");
        }

        return null;
    }
}
