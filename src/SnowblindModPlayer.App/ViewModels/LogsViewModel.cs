using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Linq;
using System.Windows;
using SnowblindModPlayer.Core.Services;
using SnowblindModPlayer.UI.MVVM;

namespace SnowblindModPlayer.ViewModels;

/// <summary>
/// ViewModel for Logs view - reads and displays current log file (tail-like)
/// </summary>
public class LogsViewModel : ViewModelBase
{
    private readonly ILoggingService _loggingService;
    private readonly IAppDataPathService _appDataPathService;
    
    private string _currentLogFile = string.Empty;
    private bool _isAutoRefreshEnabled = true;
    private string? _selectedLogFile;
    private ObservableCollection<string> _logFiles = new();
    private ObservableCollection<LogEntry> _logEntries = new();
    
    public string CurrentLogFile
    {
        get => _currentLogFile;
        set => SetProperty(ref _currentLogFile, value);
    }

    public ObservableCollection<string> LogFiles
    {
        get => _logFiles;
        set => SetProperty(ref _logFiles, value);
    }


    public string? SelectedLogFile
    {
        get => _selectedLogFile;
        set
        {
            SetProperty(ref _selectedLogFile, value);
            _ = LoadSelectedLogFileAsync();
        }
    }

    public ObservableCollection<LogEntry> LogEntries
    {
        get => _logEntries;
        set => SetProperty(ref _logEntries, value);
    }

    public bool IsAutoRefreshEnabled
    {
        get => _isAutoRefreshEnabled;
        set => SetProperty(ref _isAutoRefreshEnabled, value);
    }

    public RelayCommand RefreshCommand { get; }
    public RelayCommand ClearLogsCommand { get; }
    public RelayCommand OpenLogsFolder { get; }
    

    public LogsViewModel(ILoggingService loggingService, IAppDataPathService appDataPathService)
    {
        _loggingService = loggingService;
        _appDataPathService = appDataPathService;
        
        RefreshCommand = new RelayCommand(_ => RefreshLogsAsync());
        ClearLogsCommand = new RelayCommand(_ => ClearLogsAsync());
        OpenLogsFolder = new RelayCommand(_ => OpenLogsFolderExecute());
        
        System.Diagnostics.Debug.WriteLine("?? LogsViewModel created");
        
        // Load initial logs
        _ = RefreshLogsAsync();
    }

    private async Task RefreshLogsAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("?? Refreshing logs...");

            var logFiles = _loggingService.GetLogFileNames()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            LogFiles = new ObservableCollection<string>(logFiles);

            if (LogFiles.Count == 0)
            {
                LogEntries = new ObservableCollection<LogEntry>
                {
                    new LogEntry("No logs available yet. Try importing a video or changing settings.", LogEntryLevel.Info)
                };
                CurrentLogFile = string.Empty;
                return;
            }

            if (SelectedLogFile == null || !LogFiles.Contains(SelectedLogFile))
            {
                SelectedLogFile = LogFiles.FirstOrDefault();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Failed to load logs: {ex.Message}");
            LogEntries = new ObservableCollection<LogEntry>
            {
                new LogEntry($"Error loading logs: {ex.Message}", LogEntryLevel.Error)
            };
        }
    }


    private async Task LoadSelectedLogFileAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedLogFile))
            return;

        try
        {
            var logsPath = _appDataPathService.GetLogsFolder();
            var logFile = Path.Combine(logsPath, SelectedLogFile);
            CurrentLogFile = logFile;

            var lines = new List<string>();
            using (var stream = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    lines.Add(line);
                }
            }

            var display = lines.Count > 1000
                ? lines.Skip(lines.Count - 1000).ToList()
                : lines;

            LogEntries = new ObservableCollection<LogEntry>(display.Select(ParseLogLine));
        }
        catch (Exception ex)
        {
            LogEntries = new ObservableCollection<LogEntry>
            {
                new LogEntry($"Error reading log file: {ex.Message}", LogEntryLevel.Error)
            };
        }
    }

    private LogEntry ParseLogLine(string line)
    {
        if (line.Contains("[DBG]", StringComparison.OrdinalIgnoreCase))
            return new LogEntry(line, LogEntryLevel.Debug);
        if (line.Contains("[WRN]", StringComparison.OrdinalIgnoreCase))
            return new LogEntry(line, LogEntryLevel.Warn);
        if (line.Contains("[ERR]", StringComparison.OrdinalIgnoreCase))
            return new LogEntry(line, LogEntryLevel.Error);
        if (line.Contains("[FTL]", StringComparison.OrdinalIgnoreCase))
            return new LogEntry(line, LogEntryLevel.Critical);

        return new LogEntry(line, LogEntryLevel.Info);
    }

    private async Task ClearLogsAsync()
    {
        try
        {
            var logsPath = _appDataPathService.GetLogsFolder();
            var logFileName = SelectedLogFile ?? _loggingService.GetLogFileNames().FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(logFileName))
            {
                var logFile = Path.Combine(logsPath, logFileName);
                if (File.Exists(logFile))
                    File.Delete(logFile);
                System.Diagnostics.Debug.WriteLine("? Log file cleared");
                LogEntries = new ObservableCollection<LogEntry>
                {
                    new LogEntry($"Log file cleared at {DateTime.Now:HH:mm:ss}", LogEntryLevel.Info)
                };
                
                // Refresh to show new empty state
                await Task.Delay(500);
                await RefreshLogsAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Failed to clear logs: {ex.Message}");
            LogEntries = new ObservableCollection<LogEntry>
            {
                new LogEntry($"Error clearing logs: {ex.Message}", LogEntryLevel.Error)
            };
        }
    }

    private void OpenLogsFolderExecute()
    {
        try
        {
            var logsPath = _appDataPathService.GetLogsFolder();
            System.Diagnostics.Process.Start("explorer.exe", logsPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Failed to open logs folder: {ex.Message}");
        }
    }
}
