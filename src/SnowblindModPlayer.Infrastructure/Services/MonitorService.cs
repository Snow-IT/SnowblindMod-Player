using SnowblindModPlayer.Core.Services;

namespace SnowblindModPlayer.Infrastructure.Services;

public class MonitorService : IMonitorService
{
    private string? _selectedMonitorId;
    private readonly ISettingsService _settingsService;
    private const string SelectedMonitorKey = "SelectedMonitorId";

    public MonitorService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        LoadSelectedMonitor();
    }

    public IReadOnlyList<MonitorInfo> GetAvailableMonitors()
    {
        var monitors = new List<MonitorInfo>();
        int displayNumber = 1;

        try
        {
            // Use reflection to get screens to avoid requiring System.Windows.Forms reference
            var screenType = Type.GetType("System.Windows.Forms.Screen, System.Windows.Forms");
            if (screenType == null)
            {
                // Fallback: return primary monitor info only
                var primaryMonitor = new MonitorInfo
                {
                    Id = "Primary",
                    DisplayName = "Primary Display",
                    X = 0,
                    Y = 0,
                    Width = 1920,
                    Height = 1080,
                    IsPrimary = true
                };
                return new List<MonitorInfo> { primaryMonitor }.AsReadOnly();
            }

            var allScreensProperty = screenType.GetProperty("AllScreens");
            var screens = allScreensProperty?.GetValue(null) as Array;

            if (screens == null)
                return new List<MonitorInfo>().AsReadOnly();

            foreach (var screenObj in screens)
            {
                var deviceNameProp = screenType.GetProperty("DeviceName");
                var boundsProperty = screenType.GetProperty("Bounds");
                var primaryProperty = screenType.GetProperty("Primary");

                var deviceName = deviceNameProp?.GetValue(screenObj) as string ?? "";
                var bounds = (System.Drawing.Rectangle?)boundsProperty?.GetValue(screenObj) ?? System.Drawing.Rectangle.Empty;
                var isPrimary = (bool?)primaryProperty?.GetValue(screenObj) ?? false;

                var monitorId = deviceName.Replace(@"\\.\", "").Replace(@"\", "_");

                monitors.Add(new MonitorInfo
                {
                    Id = monitorId,
                    DisplayName = displayNumber > 1 ? $"Display {displayNumber}" : "Primary Display",
                    X = bounds.X,
                    Y = bounds.Y,
                    Width = bounds.Width,
                    Height = bounds.Height,
                    IsPrimary = isPrimary
                });

                displayNumber++;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to get monitors: {ex.Message}");
        }

        return monitors.AsReadOnly();
    }

    public MonitorInfo? GetMonitorById(string monitorId)
    {
        var monitors = GetAvailableMonitors();
        return monitors.FirstOrDefault(x => x.Id == monitorId);
    }

    public void SelectMonitor(string monitorId)
    {
        var monitor = GetMonitorById(monitorId);
        if (monitor != null)
        {
            _selectedMonitorId = monitorId;
            _settingsService.Set(SelectedMonitorKey, monitorId);
        }
    }

    public MonitorInfo? GetSelectedMonitor()
    {
        if (string.IsNullOrEmpty(_selectedMonitorId))
            return null;

        return GetMonitorById(_selectedMonitorId);
    }

    private void LoadSelectedMonitor()
    {
        _selectedMonitorId = _settingsService.Get(SelectedMonitorKey, string.Empty);
        
        // Validate that the selected monitor still exists
        if (!string.IsNullOrEmpty(_selectedMonitorId))
        {
            if (GetMonitorById(_selectedMonitorId) == null)
            {
                // Monitor no longer available, reset selection
                _selectedMonitorId = null;
                _settingsService.Set(SelectedMonitorKey, string.Empty);
            }
        }
    }
}
