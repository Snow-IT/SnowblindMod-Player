using SnowblindModPlayer.Core.Services;

namespace SnowblindModPlayer.Infrastructure.Services;

public class AppDataPathService : IAppDataPathService
{
    private const string AppName = "SnowblindModPlayer";
    private readonly string _appDataRoot;

    public AppDataPathService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _appDataRoot = Path.Combine(appDataPath, AppName);
    }

    public string GetAppDataRoot() => _appDataRoot;

    public string GetSettingsFilePath() => Path.Combine(_appDataRoot, "settings.json");

    public string GetLibraryDbPath() => Path.Combine(_appDataRoot, "library.db");

    public string GetMediaFolder() => Path.Combine(_appDataRoot, "media");

    public string GetLogsFolder() => Path.Combine(_appDataRoot, "Logs");

    public void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(_appDataRoot);
        Directory.CreateDirectory(GetMediaFolder());
        Directory.CreateDirectory(GetLogsFolder());
    }
}
