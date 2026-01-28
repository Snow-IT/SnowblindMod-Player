using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows;
using Microsoft.Win32;
using SnowblindModPlayer.Core.Services;
using SnowblindModPlayer.Infrastructure.Services;
using SnowblindModPlayer.UI.MVVM;
using SnowblindModPlayer.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace SnowblindModPlayer.ViewModels;

public class VideosViewModel : ViewModelBase
{
    private readonly ILibraryService _libraryService;
    private readonly IImportService _importService;
    private readonly ISettingsService _settingsService;
    private readonly IPlaybackService _playbackService;
    private readonly IServiceProvider _serviceProvider;
    private readonly INotificationOrchestrator _notifier;
    private ObservableCollection<MediaItem> _videos = new();
    private PlayerWindow? _activePlayerWindow;
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
        IImportService importService,
        ISettingsService settingsService,
        IPlaybackService playbackService,
        IServiceProvider serviceProvider,
        INotificationOrchestrator notifier)
    {
        _libraryService = libraryService;
        _importService = importService;
        _settingsService = settingsService;
        _playbackService = playbackService;
        _serviceProvider = serviceProvider;
        _notifier = notifier;

        _filteredVideos = CollectionViewSource.GetDefaultView(_videos);
        _filteredVideos.Filter = FilterVideo;
        var persisted = _settingsService.GetVideosViewMode();
        _viewMode = _viewModes.Contains(persisted) ? persisted : _viewModes[0];

        ImportCommand = new RelayCommand(_ => ImportVideosAsync());
        RemoveCommand = new RelayCommand(_ => RemoveSelectedAsync());
        SetDefaultCommand = new RelayCommand(_ => SetAsDefaultAsync());
        PlaySelectedCommand = new RelayCommand(_ => PlaySelectedAsync());
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

            var importedMedia = await _importService.ImportMediaAsync(dialog.FileNames);

            if (importedMedia.Count > 0)
            {
                await LoadVideosAsync();
                await _notifier.NotifyAsync($"Imported {importedMedia.Count} video(s)", NotificationScenario.ImportSuccess, NotificationType.Success);
            }
            else
            {
                await _notifier.NotifyAsync("No videos were imported (invalid or duplicate)", NotificationScenario.ImportError, NotificationType.Warning);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Import error: {ex.Message}");
            await _notifier.NotifyAsync($"Import failed: {ex.Message}", NotificationScenario.ImportError, NotificationType.Error);
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

        try
        {
            await _libraryService.RemoveMediaAsync(mediaToRemove.Id);
            
            // Clear selection BEFORE reloading videos
            SelectedMedia = null;
            
            await LoadVideosAsync();
            await _notifier.NotifyAsync($"Removed: {mediaToRemove.DisplayName}", NotificationScenario.RemoveSuccess, NotificationType.Success);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to remove video: {ex.Message}");
            await _notifier.NotifyAsync($"Failed to remove: {ex.Message}", NotificationScenario.RemoveError, NotificationType.Error);
        }
    }

    private async Task SetAsDefaultAsync()
    {
        if (SelectedMedia == null)
        {
            await _notifier.NotifyAsync("Please select a video first", NotificationScenario.PlaybackError, NotificationType.Warning);
            return;
        }

        try
        {
            var selectedId = SelectedMedia.Id;
            var selectedName = SelectedMedia.DisplayName;

            await _libraryService.SetDefaultVideoAsync(selectedId);
            DefaultVideoId = selectedId;
            _ = _settingsService.SaveAsync();
            await LoadVideosAsync();

            await _notifier.NotifyAsync($"Default set: {selectedName}", NotificationScenario.DefaultVideoSet, NotificationType.Success);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to set default video: {ex.Message}");
            await _notifier.NotifyAsync($"Failed to set default: {ex.Message}", NotificationScenario.PlaybackError, NotificationType.Error);
        }
    }

    private async void PlaySelectedAsync()
    {
        if (SelectedMedia == null)
            return;

        // Validate file exists before trying to play
        if (string.IsNullOrEmpty(SelectedMedia.StoredPath) || !File.Exists(SelectedMedia.StoredPath))
        {
            await _notifier.NotifyAsync(
                $"Video file not found: {SelectedMedia.DisplayName}", 
                NotificationScenario.PlaybackMissingFile, 
                NotificationType.Error);
            return;
        }

        try
        {
            if (_activePlayerWindow == null || !_activePlayerWindow.IsLoaded)
            {
                var playerWindow = _serviceProvider.GetRequiredService<PlayerWindow>();
                var vm = _serviceProvider.GetRequiredService<PlayerWindowViewModel>();
                playerWindow.DataContext = vm;
                playerWindow.Closed += (s, e) => _activePlayerWindow = null;
                _activePlayerWindow = playerWindow;
                playerWindow.Show();
            }

            _activePlayerWindow.Activate();
            _activePlayerWindow.Focus();
            await _activePlayerWindow.LoadVideoAsync(SelectedMedia.StoredPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Playback error: {ex.Message}");
            await _notifier.NotifyAsync(
                $"Playback failed: {ex.Message}", 
                NotificationScenario.PlaybackError, 
                NotificationType.Error);
        }
    }
}

