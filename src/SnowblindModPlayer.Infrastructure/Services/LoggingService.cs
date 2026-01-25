using SnowblindModPlayer.Core.Services;

namespace SnowblindModPlayer.Infrastructure.Services;

public class LoggingService : ILoggingService
{
    private readonly IAppDataPathService _appDataPathService;

    public LoggingService(IAppDataPathService appDataPathService)
    {
        _appDataPathService = appDataPathService;
    }

    public void Log(LogLevel level, string module, string message, Exception? exception = null)
    {
        // TODO: Implement Serilog integration
    }

    public IReadOnlyList<string> GetLogFileNames()
    {
        var logsFolder = _appDataPathService.GetLogsFolder();
        if (!Directory.Exists(logsFolder))
            return new List<string>();

        return Directory.GetFiles(logsFolder, "*.log")
            .Select(Path.GetFileName)
            .Where(x => x != null)
            .Cast<string>()
            .OrderByDescending(x => x)
            .ToList();
    }

    public Task<string> ReadLogFileAsync(string fileName)
    {
        // TODO: Read log file content
        return Task.FromResult(string.Empty);
    }

    public Task DeleteLogFileAsync(string fileName)
    {
        // TODO: Delete log file with confirmation
        return Task.CompletedTask;
    }
}
