using Serilog;
using System.Text;
using SnowblindModPlayer.Core.Services;

namespace SnowblindModPlayer.Infrastructure.Services;

/// <summary>
/// Logging service - delegates to global Serilog.Log instance.
/// Serilog is initialized in App.xaml.cs OnStartup.
/// </summary>
public class LoggingService : ILoggingService
{
    private readonly IAppDataPathService _appDataPathService;

    public LoggingService(IAppDataPathService appDataPathService)
    {
        _appDataPathService = appDataPathService;
    }

    /// <summary>
    /// Log a message using the global Serilog logger.
    /// </summary>
    public void Log(LogLevel level, string module, string message, Exception? exception = null)
    {
        var fullMessage = $"[{module}] {message}";

        switch (level)
        {
            case LogLevel.Debug:
                global::Serilog.Log.Debug(fullMessage, exception);
                break;
            case LogLevel.Info:
                global::Serilog.Log.Information(fullMessage, exception);
                break;
            case LogLevel.Warn:
                global::Serilog.Log.Warning(fullMessage, exception);
                break;
            case LogLevel.Error:
                global::Serilog.Log.Error(exception, fullMessage);
                break;
            case LogLevel.Critical:
                global::Serilog.Log.Fatal(exception, fullMessage);
                break;
        }
    }

    /// <summary>
    /// Get all log file names in the logs folder.
    /// </summary>
    public IReadOnlyList<string> GetLogFileNames()
    {
        var logsFolder = _appDataPathService.GetLogsFolder();
        if (!Directory.Exists(logsFolder))
            return new List<string>();

        return Directory.GetFiles(logsFolder, "*.log")
            .Concat(Directory.GetFiles(logsFolder, "*.log.*"))
            .Select(Path.GetFileName)
            .Where(x => x != null)
            .Cast<string>()
            .OrderByDescending(x => x)
            .ToList();
    }

    /// <summary>
    /// Read a specific log file content.
    /// </summary>
    public Task<string> ReadLogFileAsync(string fileName)
    {
        try
        {
            var logsFolder = _appDataPathService.GetLogsFolder();
            var filePath = Path.Combine(logsFolder, fileName);

            if (!File.Exists(filePath))
                return Task.FromResult($"Log file not found: {fileName}");

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var content = reader.ReadToEnd();
            return Task.FromResult(content);
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Error reading log file: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete a specific log file.
    /// </summary>
    public Task DeleteLogFileAsync(string fileName)
    {
        try
        {
            var logsFolder = _appDataPathService.GetLogsFolder();
            var filePath = Path.Combine(logsFolder, fileName);

            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error deleting log file: {ex.Message}");
        }

        return Task.CompletedTask;
    }
}
