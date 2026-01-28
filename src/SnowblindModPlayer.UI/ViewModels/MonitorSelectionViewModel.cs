using System.Collections.ObjectModel;
using SnowblindModPlayer.Core.Services;
using SnowblindModPlayer.UI.MVVM;

namespace SnowblindModPlayer.UI.ViewModels;

public class MonitorSelectionViewModel : ViewModelBase
{
    private readonly IMonitorService _monitorService;
    private readonly ISettingsService _settingsService;
    private readonly INotificationOrchestrator _notifier;
    private readonly ILoggingService _logger;
    private ObservableCollection<MonitorInfo> _availableMonitors = new();
    private MonitorInfo? _selectedMonitor;

    public ObservableCollection<MonitorInfo> AvailableMonitors
    {
        get => _availableMonitors;
        set => SetProperty(ref _availableMonitors, value);
    }

    public MonitorInfo? SelectedMonitor
    {
        get => _selectedMonitor;
        set
        {
            SetProperty(ref _selectedMonitor, value);
            if (value != null)
            {
                _monitorService.SelectMonitor(value.Id);
                // Persist to disk immediately
                _ = _settingsService.SaveAsync();
                
                // Notify user
                _ = _notifier.NotifyAsync(
                    $"Display set to: {value.DisplayName}",
                    NotificationScenario.SettingsSaved,
                    NotificationType.Info);
            }
        }
    }

    public RelayCommand<MonitorInfo> SelectMonitorCommand { get; }

    public MonitorSelectionViewModel(
        IMonitorService monitorService, 
        ISettingsService settingsService,
        INotificationOrchestrator notifier,
        ILoggingService logger)
    {
        _monitorService = monitorService;
        _settingsService = settingsService;
        _notifier = notifier;
        _logger = logger;
        SelectMonitorCommand = new RelayCommand<MonitorInfo>(SelectMonitorExecute);
        LoadMonitors();
    }

    private void LoadMonitors()
    {
        var monitors = _monitorService.GetAvailableMonitors();
        AvailableMonitors = new ObservableCollection<MonitorInfo>(monitors);
        
        var selected = _monitorService.GetSelectedMonitor();
        if (selected != null)
        {
            _selectedMonitor = monitors.FirstOrDefault(m => m.Id == selected.Id);
            OnPropertyChanged(nameof(SelectedMonitor));
        }
    }

    private void SelectMonitorExecute(MonitorInfo? monitor)
    {
        if (monitor != null)
        {
            SelectedMonitor = monitor;
        }
    }

    public void RefreshMonitors()
    {
        LoadMonitors();
    }
}
