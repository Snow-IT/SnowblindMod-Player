namespace SnowblindModPlayer.Core.Services;

public interface ILoggingService
{
    void Log(LogLevel level, string module, string message, Exception? exception = null);
    IReadOnlyList<string> GetLogFileNames();
    Task<string> ReadLogFileAsync(string fileName);
    Task DeleteLogFileAsync(string fileName);
}

public enum LogLevel
{
    Debug,
    Info,
    Warn,
    Error
}
