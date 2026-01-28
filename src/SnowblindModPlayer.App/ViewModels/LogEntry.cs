namespace SnowblindModPlayer.ViewModels;

public enum LogEntryLevel
{
    Debug,
    Info,
    Warn,
    Error,
    Critical
}

public class LogEntry
{
    public string Text { get; }
    public LogEntryLevel Level { get; }

    public LogEntry(string text, LogEntryLevel level)
    {
        Text = text;
        Level = level;
    }
}
