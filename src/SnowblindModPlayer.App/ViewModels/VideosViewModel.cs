using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Win32;
using SnowblindModPlayer.Core.Services;
using SnowblindModPlayer.UI.MVVM;

namespace SnowblindModPlayer.ViewModels;

public class VideosViewModel : ViewModelBase
{
    private readonly ILibraryService _libraryService;
    private readonly IImportService _importService;
    private ObservableCollection<MediaItem> _videos = new();
    private MediaItem? _selectedMedia;

    public ObservableCollection<MediaItem> Videos
    {
        get => _videos;
        set => SetProperty(ref _videos, value);
    }

    public MediaItem? SelectedMedia
    {
        get => _selectedMedia;
        set => SetProperty(ref _selectedMedia, value);
    }

    public RelayCommand ImportCommand { get; }
    public RelayCommand RemoveCommand { get; }
    public RelayCommand SetDefaultCommand { get; }

    public VideosViewModel(ILibraryService libraryService, IImportService importService)
    {
        _libraryService = libraryService;
        _importService = importService;

        ImportCommand = new RelayCommand(_ => ImportVideosAsync());
        RemoveCommand = new RelayCommand(_ => RemoveSelectedAsync());
        SetDefaultCommand = new RelayCommand(_ => SetAsDefaultAsync());

        _ = LoadVideosAsync();
    }

    private async Task LoadVideosAsync()
    {
        try
        {
            var mediaItems = await _libraryService.GetAllMediaAsync();
            Videos = new ObservableCollection<MediaItem>(mediaItems);
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

            System.Diagnostics.Debug.WriteLine($"Importing {dialog.FileNames.Length} video(s)...");

            var importedMedia = await _importService.ImportMediaAsync(dialog.FileNames);

            if (importedMedia.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"Successfully imported {importedMedia.Count} video(s)");
                await LoadVideosAsync();
                MessageBox.Show(
                    $"Successfully imported {importedMedia.Count} video(s)",
                    "Import Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(
                    "No videos were imported. Check that files are valid and not duplicates.",
                    "Import Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Import error: {ex.Message}");
            MessageBox.Show(
                $"Import failed: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task RemoveSelectedAsync()
    {
        if (SelectedMedia == null)
            return;

        var result = MessageBox.Show(
            $"Are you sure you want to remove \"{SelectedMedia.DisplayName}\"?\n\nThe file will be permanently deleted.",
            "Confirm Removal",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            await _libraryService.RemoveMediaAsync(SelectedMedia.Id);
            await LoadVideosAsync();
            MessageBox.Show(
                "Video removed successfully",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to remove video: {ex.Message}");
            MessageBox.Show(
                $"Failed to remove video: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task SetAsDefaultAsync()
    {
        if (SelectedMedia == null)
        {
            MessageBox.Show(
                "Please select a video first",
                "No Selection",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        try
        {
            var selectedId = SelectedMedia.Id;
            var selectedName = SelectedMedia.DisplayName; // Capture name before reload
            
            await _libraryService.SetDefaultVideoAsync(selectedId);
            await LoadVideosAsync();
            
            MessageBox.Show(
                $"\"{selectedName}\" is now set as the default video",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to set default video: {ex.Message}");
            MessageBox.Show(
                $"Failed to set default video: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
