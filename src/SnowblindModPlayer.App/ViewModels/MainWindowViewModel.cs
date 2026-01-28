using SnowblindModPlayer.Core.Services;
using SnowblindModPlayer.Infrastructure.Services;
using SnowblindModPlayer.Services;
using SnowblindModPlayer.UI.MVVM;
using SnowblindModPlayer.Views;
using SnowblindModPlayer.ViewModels;

namespace SnowblindModPlayer.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly ILibraryService _libraryService;
    private readonly ISettingsService _settingsService;
    private readonly VideosView _videosView;
    private readonly LogsView _logsView;
    private readonly SettingsView _settingsView;

    public RelayCommand NavigateVideosCommand { get; }
    public RelayCommand NavigateLogsCommand { get; }
    public RelayCommand NavigateSettingsCommand { get; }
    public RelayCommand ToggleSidebarCommand { get; }

    private bool _sidebarCollapsed;
    public bool SidebarCollapsed
    {
        get => _sidebarCollapsed;
        set
        {
            var oldValue = _sidebarCollapsed;
            SetProperty(ref _sidebarCollapsed, value);
            if (oldValue != value)
            {
                _settingsService.SetSidebarCollapsed(value);
                _ = _settingsService.SaveAsync();
            }
        }
    }

    private object? _commandBarContent;
    public object? CommandBarContent
    {
        get => _commandBarContent;
        set => SetProperty(ref _commandBarContent, value);
    }

    private string _pageTitle = "Videos";
    public string PageTitle
    {
        get => _pageTitle;
        set => SetProperty(ref _pageTitle, value);
    }

    private object? _currentView;
    public object? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    private string _selectedPage = "Videos";
    public string SelectedPage
    {
        get => _selectedPage;
        set
        {
            SetProperty(ref _selectedPage, value);
            UpdateCurrentView();
        }
    }

    public MainWindowViewModel(
        ILibraryService libraryService,
        ISettingsService settingsService,
        VideosView videosView,
        LogsView logsView,
        SettingsView settingsView,
        LogsViewModel logsViewModel)
    {
        _libraryService = libraryService;
        _settingsService = settingsService;
        _videosView = videosView;
        _logsView = logsView;
        _settingsView = settingsView;

        // Wire LogsViewModel to LogsView (before binding)
        _logsView.DataContext = logsViewModel;

        // Load sidebar collapsed state from settings
        _sidebarCollapsed = _settingsService.GetSidebarCollapsed();

        NavigateVideosCommand = new RelayCommand(_ => SelectedPage = "Videos");
        NavigateLogsCommand = new RelayCommand(_ => SelectedPage = "Logs");
        NavigateSettingsCommand = new RelayCommand(_ => SelectedPage = "Settings");
        ToggleSidebarCommand = new RelayCommand(_ => SidebarCollapsed = !SidebarCollapsed);

        UpdateCurrentView();
    }

    private void UpdateCurrentView()
    {
        CurrentView = SelectedPage switch
        {
            "Logs" => _logsView,
            "Settings" => _settingsView,
            _ => _videosView,
        };

        PageTitle = SelectedPage;
        CommandBarContent = CommandBarFactory.CreateForPage(SelectedPage, CurrentView);
    }
}
