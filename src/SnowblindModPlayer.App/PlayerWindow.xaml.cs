using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Interop;
using SnowblindModPlayer.Core.Services;
using SnowblindModPlayer.UI.ViewModels;

namespace SnowblindModPlayer;

public partial class PlayerWindow : Window
{
    private readonly IPlaybackService _playbackService;
    private readonly IMonitorService _monitorService;
    private readonly PlayerWindowViewModel _viewModel;
    private bool _isFullscreen;
    private DispatcherTimer? _updateTimer;
    private string _currentVideoPath = string.Empty;

    public PlayerWindow(IPlaybackService playbackService, IMonitorService monitorService, PlayerWindowViewModel viewModel)
    {
        InitializeComponent();
        _playbackService = playbackService;
        _monitorService = monitorService;
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Set the window handle for the playback service
        var handle = new WindowInteropHelper(this).Handle;
        _playbackService.SetWindowHandle(handle);

        // Subscribe to playback events
        _playbackService.PlayingStateChanged += PlaybackService_PlayingStateChanged;
        _playbackService.PlaybackPositionChanged += PlaybackService_PlaybackPositionChanged;
        _playbackService.MediaEnded += PlaybackService_MediaEnded;

        // Position window on selected monitor (or primary if none selected)
        PositionOnSelectedMonitor();

        // Setup update timer for progress bar
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _updateTimer.Tick += UpdateTimer_Tick;
        _updateTimer.Start();
    }

    private void PositionOnSelectedMonitor()
    {
        var selectedMonitor = _monitorService.GetSelectedMonitor();
        if (selectedMonitor != null)
        {
            // Position window at the center of the selected monitor
            Left = selectedMonitor.X + (selectedMonitor.Width - Width) / 2;
            Top = selectedMonitor.Y + (selectedMonitor.Height - Height) / 2;
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _updateTimer?.Stop();
        _ = _playbackService.StopAsync();
    }

    #region Hotkey Handling

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Space:
                e.Handled = true;
                PlayPauseAsync();
                break;

            case Key.Left:
                e.Handled = true;
                if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    SeekRelativeAsync(-30000); // -30s
                else
                    SeekRelativeAsync(-5000); // -5s
                break;

            case Key.Right:
                e.Handled = true;
                if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    SeekRelativeAsync(30000); // +30s
                else
                    SeekRelativeAsync(5000); // +5s
                break;

            case Key.Up:
                e.Handled = true;
                ChangeVolumeAsync(5);
                break;

            case Key.Down:
                e.Handled = true;
                ChangeVolumeAsync(-5);
                break;

            case Key.M:
                e.Handled = true;
                ToggleMuteAsync();
                break;

            case Key.F11:
                e.Handled = true;
                ToggleFullscreenAsync();
                break;

            case Key.Escape:
                e.Handled = true;
                if (_isFullscreen)
                {
                    ToggleFullscreenAsync();
                }
                else
                {
                    ClosePlayer();
                }
                break;
        }
    }

    #endregion

    #region Mouse Handling

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && e.ClickCount == 1)
        {
            PlayPauseAsync();
            e.Handled = true;
        }
    }

    private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            ToggleFullscreenAsync();
            e.Handled = true;
        }
    }

    private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        int volumeChange = e.Delta > 0 ? 5 : -5;
        ChangeVolumeAsync(volumeChange);
        e.Handled = true;
    }

    private void ProgressBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_playbackService.DurationMs > 0)
        {
            Point clickPosition = e.GetPosition(ProgressBar);
            double progress = clickPosition.X / ProgressBar.ActualWidth;
            long newPosition = (long)(_playbackService.DurationMs * progress);
            SeekAsync(newPosition);
            e.Handled = true;
        }
    }

    #endregion

    #region Playback Control

    private async void PlayPauseAsync()
    {
        try
        {
            if (_playbackService.IsPlaying)
            {
                await _playbackService.PauseAsync();
            }
            else
            {
                if (string.IsNullOrEmpty(_currentVideoPath))
                {
                    ShowError("No video loaded");
                }
                else
                {
                    await _playbackService.ResumeAsync();
                }
            }
        }
        catch (Exception ex)
        {
            ShowError($"Playback error: {ex.Message}");
        }
    }

    private async void SeekAsync(long positionMs)
    {
        try
        {
            await _playbackService.SeekAsync(positionMs);
        }
        catch (Exception ex)
        {
            ShowError($"Seek error: {ex.Message}");
        }
    }

    private void SeekRelativeAsync(long deltaMs)
    {
        long newPosition = Math.Max(0, _playbackService.CurrentPositionMs + deltaMs);
        SeekAsync(newPosition);
    }

    private async void ChangeVolumeAsync(int deltaPercent)
    {
        int newVolume = Math.Clamp(_playbackService.VolumePercent + deltaPercent, 0, 100);
        await _playbackService.SetVolumeAsync(newVolume);
        VolumeSlider.Value = newVolume;
        VolumeLabel.Text = $"{newVolume}%";
    }

    private async void ToggleMuteAsync()
    {
        bool newMuteState = !_playbackService.IsMuted;
        await _playbackService.SetMuteAsync(newMuteState);
        VolumeSlider.IsEnabled = !newMuteState;
    }

    private void ToggleFullscreenAsync()
    {
        _isFullscreen = !_isFullscreen;
        if (_isFullscreen)
        {
            var selectedMonitor = _monitorService.GetSelectedMonitor();
            if (selectedMonitor != null)
            {
                // Fullscreen on selected monitor
                Left = selectedMonitor.X;
                Top = selectedMonitor.Y;
                Width = selectedMonitor.Width;
                Height = selectedMonitor.Height;
            }

            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
        }
        else
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            WindowState = WindowState.Normal;
            Width = 900;
            Height = 600;
            PositionOnSelectedMonitor();
        }
    }

    public async Task LoadVideoAsync(string videoPath)
    {
        try
        {
            ErrorMessage.Text = string.Empty;
            _currentVideoPath = videoPath;
            await _playbackService.PlayAsync(videoPath);
            _viewModel.CurrentVideoPath = videoPath;
        }
        catch (Exception ex)
        {
            ShowError($"Failed to load video: {ex.Message}");
        }
    }

    private void ClosePlayer()
    {
        _updateTimer?.Stop();
        Close();
    }

    private void ShowError(string message)
    {
        ErrorMessage.Text = message;
    }

    #endregion

    #region UI Updates

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            long currentTime = _playbackService.CurrentPositionMs;
            long totalTime = _playbackService.DurationMs;

            if (totalTime > 0)
            {
                ProgressBar.Maximum = totalTime;
                ProgressBar.Value = currentTime;

                TimeLabel.Text = $"{FormatTime(currentTime)} / {FormatTime(totalTime)}";
            }
        }
        catch
        {
            // Ignore timer tick errors
        }
    }

    private string FormatTime(long milliseconds)
    {
        var timespan = TimeSpan.FromMilliseconds(milliseconds);
        return $"{(int)timespan.TotalMinutes}:{timespan.Seconds:D2}";
    }

    #endregion

    #region Playback Service Events

    private void PlaybackService_PlayingStateChanged(object? sender, EventArgs e)
    {
        if (_playbackService.IsPlaying)
        {
            PlayPauseButton.Content = "?"; // Pause icon
        }
        else
        {
            PlayPauseButton.Content = "?"; // Play icon
        }
    }

    private void PlaybackService_PlaybackPositionChanged(object? sender, EventArgs e)
    {
        // UI will be updated by the timer tick
    }

    private void PlaybackService_MediaEnded(object? sender, EventArgs e)
    {
        bool loopEnabled = _viewModel.LoopEnabled;
        if (loopEnabled && !string.IsNullOrEmpty(_currentVideoPath))
        {
            _ = _playbackService.PlayAsync(_currentVideoPath);
        }
    }

    #endregion

    #region Button Clicks

    private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
    {
        PlayPauseAsync();
    }

    private void FullscreenButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleFullscreenAsync();
    }

    #endregion
}
