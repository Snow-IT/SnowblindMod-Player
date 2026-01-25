namespace SnowblindModPlayer.Core.Services;

public interface IMonitorService
{
    IReadOnlyList<MonitorInfo> GetAvailableMonitors();
    MonitorInfo? GetMonitorById(string monitorId);
    void SelectMonitor(string monitorId);
    MonitorInfo? GetSelectedMonitor();
}

public class MonitorInfo
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool IsPrimary { get; set; }
}
