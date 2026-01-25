namespace SnowblindModPlayer.Core.Services;

public interface IAppDataPathService
{
    string GetAppDataRoot();
    string GetSettingsFilePath();
    string GetLibraryDbPath();
    string GetMediaFolder();
    string GetLogsFolder();
    void EnsureDirectoriesExist();
}
