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
        try
        {
            LoadSelectedMonitor();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MonitorService initialization warning: {ex.Message}");
            // Don't throw - just continue with no selected monitor
        }
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
                System.Diagnostics.Debug.WriteLine("System.Windows.Forms.Screen not available - using fallback");
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

            if (screens == null || screens.Length == 0)
            {
                System.Diagnostics.Debug.WriteLine("No screens found - using fallback");
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

            foreach (var screenObj in screens)
            {
                try
                {
                    var deviceNameProp = screenType.GetProperty("DeviceName");
                    var boundsProperty = screenType.GetProperty("Bounds");
                    var primaryProperty = screenType.GetProperty("Primary");

                    var deviceName = deviceNameProp?.GetValue(screenObj) as string ?? "";
                    var boundsObj = boundsProperty?.GetValue(screenObj);
                    var isPrimary = (bool?)primaryProperty?.GetValue(screenObj) ?? false;

                    // Extract bounds coordinates
                    int x = 0, y = 0, width = 1920, height = 1080;
                    if (boundsObj != null)
                    {
                        var boundsType = boundsObj.GetType();
                        x = (int?)boundsType.GetProperty("X")?.GetValue(boundsObj) ?? 0;
                        y = (int?)boundsType.GetProperty("Y")?.GetValue(boundsObj) ?? 0;
                        width = (int?)boundsType.GetProperty("Width")?.GetValue(boundsObj) ?? 1920;
                        height = (int?)boundsType.GetProperty("Height")?.GetValue(boundsObj) ?? 1080;
                    }

                    var monitorId = !string.IsNullOrEmpty(deviceName) 
                        ? deviceName.Replace(@"\\.\", "").Replace(@"\", "_")
                        : $"Monitor_{displayNumber}";

                    monitors.Add(new MonitorInfo
                    {
                        Id = monitorId,
                        DisplayName = displayNumber > 1 ? $"Display {displayNumber}" : "Primary Display",
                        X = x,
                        Y = y,
                        Width = width,
                        Height = height,
                        IsPrimary = isPrimary
                    });

                    displayNumber++;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error processing screen: {ex.Message}");
                    // Continue with next screen
                }
            }

            if (monitors.Count == 0)
            {
                // Fallback if no monitors were processed
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
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetAvailableMonitors failed: {ex.Message}");
            // Return fallback monitor
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
