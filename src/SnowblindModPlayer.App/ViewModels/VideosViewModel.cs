using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows;
using Microsoft.Win32;
using SnowblindModPlayer.Core.Services;
using SnowblindModPlayer.Infrastructure.Services;
using SnowblindModPlayer.Services;
using SnowblindModPlayer.UI.MVVM;
using SnowblindModPlayer.UI.ViewModels;

namespace SnowblindModPlayer.ViewModels;

public class VideosViewModel : ViewModelBase
{
    private readonly ILibraryService _libraryService;
    private readonly ILibraryOrchestrator _libraryOrchestrator;
    private readonly ILibraryChangeNotifier _changeNotifier;
    private readonly ISettingsService _settingsService;
    private readonly PlaybackOrchestrator _playbackOrchestrator;
    private readonly INotificationOrchestrator _notifier;
    private ObservableCollection<MediaItem> _videos = new();
    private MediaItem? _selectedMedia;
    private readonly ICollectionView _filteredVideos;
    private string _searchText = string.Empty;
    private string _viewMode;
    private ObservableCollection<string> _viewModes = new() { "Tile", "List" };

    public ObservableCollection<MediaItem> Videos
    {
        get => _videos;
        set => SetProperty(ref _videos, value);
    }

    public ICollectionView FilteredVideos => _filteredVideos;

    public string SearchText
    {
        get => _searchText;
        set
        {
            SetProperty(ref _searchText, value);
            _filteredVideos.Refresh();
        }
    }

    public ObservableCollection<string> ViewModes
    {
        get => _viewModes;
        set => SetProperty(ref _viewModes, value);
    }

    public string ViewMode
    {
        get => _viewMode;
        set
        {
            if (string.Equals(_viewMode, value, StringComparison.Ordinal))
                return;

            SetProperty(ref _viewMode, value);
            _settingsService.SetVideosViewMode(_viewMode);
            _ = _settingsService.SaveAsync();
        }
    }

    public MediaItem? SelectedMedia
    {
        get => _selectedMedia;
        set => SetProperty(ref _selectedMedia, value);
    }

    private string _defaultVideoId = string.Empty;
    public string DefaultVideoId
    {
        get => _defaultVideoId;
        private set => SetProperty(ref _defaultVideoId, value);
    }

    public RelayCommand ImportCommand { get; }
    public RelayCommand RemoveCommand { get; }
    public RelayCommand SetDefaultCommand { get; }
    public RelayCommand PlaySelectedCommand { get; }

    public VideosViewModel(
        ILibraryService libraryService,
        ILibraryOrchestrator libraryOrchestrator,
        ILibraryChangeNotifier changeNotifier,
        ISettingsService settingsService,
        PlaybackOrchestrator playbackOrchestrator,
        INotificationOrchestrator notifier)
    {
        _libraryService = libraryService;
        _libraryOrchestrator = libraryOrchestrator;
        _changeNotifier = changeNotifier;
        _settingsService = settingsService;
        _playbackOrchestrator = playbackOrchestrator;
        _notifier = notifier;

        _filteredVideos = CollectionViewSource.GetDefaultView(_videos);
        _filteredVideos.Filter = FilterVideo;
        var persisted = _settingsService.GetVideosViewMode();
        _viewMode = _viewModes.Contains(persisted) ? persisted : _viewModes[0];

        ImportCommand = new RelayCommand(_ => ImportVideosAsync());
        RemoveCommand = new RelayCommand(_ => RemoveSelectedAsync());
        SetDefaultCommand = new RelayCommand(_ => SetAsDefaultAsync());
        PlaySelectedCommand = new RelayCommand(_ => PlaySelectedAsync());

        // Subscribe to library change notifier events (auto-reload on changes)
        _changeNotifier.VideoImported += (s, e) => _ = OnLibraryChangedAsync();
        _changeNotifier.VideoRemoved += (s, e) => _ = OnLibraryChangedAsync();
        _changeNotifier.DefaultVideoChanged += (s, e) => _ = OnLibraryChangedAsync();
    }

    private bool FilterVideo(object obj)
    {
        if (obj is not MediaItem item)
            return false;

        if (string.IsNullOrWhiteSpace(SearchText))
            return true;

        return item.DisplayName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true;
    }

    public async Task LoadVideosAsync()
    {
        try
        {
            var mediaItems = await _libraryService.GetAllMediaAsync();
            var defaultVideo = await _libraryService.GetDefaultVideoAsync();
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                _videos.Clear();
                foreach (var item in mediaItems)
                    _videos.Add(item);
                _filteredVideos.Refresh();
                
                // Update default video ID for badge visibility
                DefaultVideoId = defaultVideo?.Id ?? string.Empty;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load videos: {ex.Message}");
        }
    }

    /// <summary>
    /// Auto-reload videos when library changes (via LibraryOrchestrator events)
    /// </summary>
    private async Task OnLibraryChangedAsync()
    {
        System.Diagnostics.Debug.WriteLine("?? Library changed, reloading videos...");
        await LoadVideosAsync();
    }

    private async void ImportVideosAsync()
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select video files to import",
                Filter = "Video files (*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.webm)|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.webm|All files (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() != true || dialog.FileNames.Length == 0)
                return;

            // Delegate to LibraryOrchestrator (handles Import + Events + Notifications)
            await _libraryOrchestrator.ImportVideosAsync(dialog.FileNames);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Import error: {ex.Message}");
            await _notifier.NotifyErrorAsync($"Import failed: {ex.Message}", ex, NotificationScenario.ImportError);
        }
    }

    private async Task RemoveSelectedAsync()
    {
        if (SelectedMedia == null)
            return;

        // Cache the item before removal
        var mediaToRemove = SelectedMedia;

        var confirm = await _notifier.ShowConfirmationAsync(
            "Confirm Removal",
            $"Are you sure you want to remove \"{mediaToRemove.DisplayName}\"?\n\nThe file will be permanently deleted.");
        if (!confirm)
            return;

        // Delegate to LibraryOrchestrator (handles Remove + Events + Notifications)
        await _libraryOrchestrator.RemoveVideoAsync(mediaToRemove.Id);
        
        // Clear selection BEFORE reloading videos
        SelectedMedia = null;
    }

    private async Task SetAsDefaultAsync()
    {
        if (SelectedMedia == null)
        {
            await _notifier.NotifyAsync("Please select a video first", NotificationScenario.PlaybackError, NotificationType.Warning);
            return;
        }

        var selectedId = SelectedMedia.Id;
        var selectedName = SelectedMedia.DisplayName;

        // Delegate to LibraryOrchestrator (handles SetDefault + Events + Notifications)
        await _libraryOrchestrator.SetDefaultVideoAsync(selectedId);
    }

    private async void PlaySelectedAsync()
    {
        if (SelectedMedia == null || string.IsNullOrEmpty(SelectedMedia.StoredPath))
            return;

        try
        {
            await _playbackOrchestrator.PlayVideoAsync(SelectedMedia.Id);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Playback error: {ex.Message}");
            await _notifier.NotifyErrorAsync($"Playback failed: {ex.Message}", ex, NotificationScenario.PlaybackError);
        }
    }
}

