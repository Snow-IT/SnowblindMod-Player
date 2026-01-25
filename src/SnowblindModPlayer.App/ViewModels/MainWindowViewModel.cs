using SnowblindModPlayer.Core.Services;
using SnowblindModPlayer.UI.MVVM;
using SnowblindModPlayer.Views;

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
        SettingsView settingsView)
    {
        _libraryService = libraryService;
        _settingsService = settingsService;
        _videosView = videosView;
        _logsView = logsView;
        _settingsView = settingsView;

        NavigateVideosCommand = new RelayCommand(_ => SelectedPage = "Videos");
        NavigateLogsCommand = new RelayCommand(_ => SelectedPage = "Logs");
        NavigateSettingsCommand = new RelayCommand(_ => SelectedPage = "Settings");

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
    }
}
