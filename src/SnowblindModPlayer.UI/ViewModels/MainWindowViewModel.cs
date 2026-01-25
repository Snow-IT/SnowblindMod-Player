using SnowblindModPlayer.Core.Services;
using SnowblindModPlayer.UI.MVVM;

namespace SnowblindModPlayer.UI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly ILibraryService _libraryService;
    private readonly ISettingsService _settingsService;

    public MainWindowViewModel(ILibraryService libraryService, ISettingsService settingsService)
    {
        _libraryService = libraryService;
        _settingsService = settingsService;
    }

    private string _selectedPage = "Videos";
    public string SelectedPage
    {
        get => _selectedPage;
        set => SetProperty(ref _selectedPage, value);
    }

    private bool _sidebarCollapsed;
    public bool SidebarCollapsed
    {
        get => _sidebarCollapsed;
        set => SetProperty(ref _sidebarCollapsed, value);
    }
}
