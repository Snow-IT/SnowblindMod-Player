using Microsoft.Data.Sqlite;

namespace SnowblindModPlayer.Infrastructure.Data;

public class LibraryDbContext
{
    private readonly string _databasePath;

    public LibraryDbContext(string databasePath)
    {
        _databasePath = databasePath;
    }

    public async Task InitializeAsync()
    {
        try
        {
            using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
            {
                await connection.OpenAsync();
                
                // Create media table
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Media (
                            Id TEXT PRIMARY KEY,
                            DisplayName TEXT NOT NULL,
                            OriginalSourcePath TEXT NOT NULL UNIQUE,
                            StoredPath TEXT NOT NULL UNIQUE,
                            DateAdded TEXT NOT NULL,
                            ThumbnailPath TEXT
                        );
                    ";
                    await command.ExecuteNonQueryAsync();
                }

                // Create index on OriginalSourcePath for duplicate checking
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        CREATE INDEX IF NOT EXISTS idx_media_original_source 
                        ON Media(OriginalSourcePath);
                    ";
                    await command.ExecuteNonQueryAsync();
                }

                // Create index on StoredPath for cleanup
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        CREATE INDEX IF NOT EXISTS idx_media_stored_path 
                        ON Media(StoredPath);
                    ";
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Database initialization failed: {ex.Message}");
            throw;
        }
    }

    public SqliteConnection GetConnection()
    {
        return new SqliteConnection($"Data Source={_databasePath}");
    }
}
