using SnowblindModPlayer.Core.Services;

namespace SnowblindModPlayer.Infrastructure.Services;

public class MonitorService : IMonitorService
{
    private string? _selectedMonitorId;

    public IReadOnlyList<MonitorInfo> GetAvailableMonitors()
    {
        // TODO: Query Windows API or System.Windows.Forms.Screen for monitors
        return new List<MonitorInfo>();
    }

    public MonitorInfo? GetMonitorById(string monitorId)
    {
        var monitors = GetAvailableMonitors();
        return monitors.FirstOrDefault(x => x.Id == monitorId);
    }

    public void SelectMonitor(string monitorId)
    {
        _selectedMonitorId = monitorId;
        // TODO: Persist to settings
    }

    public MonitorInfo? GetSelectedMonitor()
    {
        if (string.IsNullOrEmpty(_selectedMonitorId))
            return null;

        return GetMonitorById(_selectedMonitorId);
    }
}
