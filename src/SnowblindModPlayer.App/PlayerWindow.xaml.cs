using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Interop;
using SnowblindModPlayer.Core.Services;
using SnowblindModPlayer.Infrastructure.Services;
using SnowblindModPlayer.UI.ViewModels;

namespace SnowblindModPlayer;

public partial class PlayerWindow : Window
{
    private readonly IPlaybackService _playbackService;
    private readonly IMonitorService _monitorService;
    private readonly ISettingsService _settingsService;
    private readonly PlayerWindowViewModel _viewModel;
    private bool _isFullscreen;
    private DispatcherTimer? _updateTimer;
    private DispatcherTimer? _osdHideTimer;

    public PlayerWindow(IPlaybackService playbackService, IMonitorService monitorService, ISettingsService settingsService, PlayerWindowViewModel viewModel)
    {
        InitializeComponent();
        _playbackService = playbackService;
        _monitorService = monitorService;
        _settingsService = settingsService;
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Bind LibVLC MediaPlayer to VideoView
        if (_playbackService is PlaybackService svc)
        {
            VideoView.MediaPlayer = svc.MediaPlayer;
            
            // Register UI dispatcher for loop restarts
            svc.SetUIDispatcher(action => Dispatcher.InvokeAsync(action));
            
            // Subscribe to EndReached for OSD display
            if (_playbackService is PlaybackService playbackSvc)
            {
                playbackSvc.MediaEndReached += (s, e) => ShowOsd("Video Ended");
            }
        }

        // Subscribe to playback events
        _playbackService.PlayingStateChanged += PlaybackService_PlayingStateChanged;
        _playbackService.PlaybackPositionChanged += PlaybackService_PlaybackPositionChanged;
        _playbackService.MediaEnded += PlaybackService_MediaEnded;
        _playbackService.VolumeChanged += PlaybackService_VolumeChanged;

        // Wire volume slider
        VolumeSlider.ValueChanged += VolumeSlider_ValueChanged;
        
        // Setup OSD hide timer
        _osdHideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _osdHideTimer.Tick += (_, __) => { Osd.Visibility = Visibility.Collapsed; _osdHideTimer.Stop(); };
        
        // Load settings
        LoadSettingsAsync();

        // Position window on selected monitor (or primary if none selected)
        PositionOnSelectedMonitor();

        // Setup update timer for progress bar and OSD
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _updateTimer.Tick += UpdateTimer_Tick;
        _updateTimer.Start();

        // Apply fullscreen if enabled (after UI is fully initialized)
        if (_settingsService.GetFullscreenOnStart())
        {
            // Defer to allow rendering to complete
            _ = Dispatcher.BeginInvoke(new Action(() => ToggleFullscreenAsync()), DispatcherPriority.ApplicationIdle);
        }
    }

    private async void LoadSettingsAsync()
    {
        // Load playback settings
        _viewModel.LoopEnabled = _settingsService.GetLoopEnabled();
        var volume = _settingsService.GetVolume();
        var muted = _settingsService.GetMuted();
        _viewModel.ScalingMode = _settingsService.GetScalingMode();

        // Apply volume and mute
        await _playbackService.SetVolumeAsync(volume);
        VolumeSlider.Value = volume;
        VolumeLabel.Text = $"{volume}%";
        
        await _playbackService.SetMuteAsync(muted);
        VolumeSlider.IsEnabled = !muted;
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

            case Key.L:
                e.Handled = true;
                ToggleLoopAsync();
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
                ShowOsd("Paused");
            }
            else
            {
                if (string.IsNullOrEmpty(_viewModel.CurrentVideoPath))
                {
                    ShowError("No video loaded");
                }
                else
                {
                    // Use PlayAsync with the last loaded path to ensure resume works after pause
                    await _playbackService.PlayAsync(_viewModel.CurrentVideoPath);
                    ShowOsd("Playing");
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
            ShowOsd($"Seek: {FormatTime(positionMs)}");
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
        ShowOsd(_playbackService.IsMuted ? "Muted" : $"Vol {newVolume}%");
    }

    private async void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_playbackService != null)
        {
            await _playbackService.SetVolumeAsync((int)e.NewValue);
        }
    }

    private void ToggleLoopAsync()
    {
        _viewModel.LoopEnabled = !_viewModel.LoopEnabled;
        if (_playbackService is SnowblindModPlayer.Infrastructure.Services.PlaybackService svc)
        {
            svc.SetLoopEnabled(_viewModel.LoopEnabled);
        }
        ShowOsd(_viewModel.LoopEnabled ? "Loop: ON" : "Loop: OFF");
    }

    private async void ToggleMuteAsync()
    {
        bool newMuteState = !_playbackService.IsMuted;
        await _playbackService.SetMuteAsync(newMuteState);
        VolumeSlider.IsEnabled = !newMuteState;
        ShowOsd(newMuteState ? "Muted" : $"Vol {_playbackService.VolumePercent}%");
    }

    private async void ToggleFullscreenAsync()
    {
        _isFullscreen = !_isFullscreen;
        if (_isFullscreen)
        {
            // Pause playback during transition to avoid rendering issues
            var wasPlaying = false;
            try { wasPlaying = _playbackService.IsPlaying; if (wasPlaying) await _playbackService.PauseAsync(); } catch { }

            // Detach renderer before size/position changes (critical for smooth transitions)
            try { VideoView.MediaPlayer = null; } catch { }

            WindowStartupLocation = WindowStartupLocation.Manual;
            WindowState = WindowState.Normal;

            var selectedMonitor = _monitorService.GetSelectedMonitor();
            if (selectedMonitor != null)
            {
                // DPI-aware conversion
                var source = PresentationSource.FromVisual(this);
                var transform = source?.CompositionTarget?.TransformFromDevice ?? System.Windows.Media.Matrix.Identity;
                var topLeft = transform.Transform(new System.Windows.Point(selectedMonitor.X, selectedMonitor.Y));
                var bottomRight = transform.Transform(new System.Windows.Point(selectedMonitor.X + selectedMonitor.Width, selectedMonitor.Y + selectedMonitor.Height));

                Left = topLeft.X;
                Top = topLeft.Y;
                Width = Math.Max(1, bottomRight.X - topLeft.X);
                Height = Math.Max(1, bottomRight.Y - topLeft.Y);
            }
            else
            {
                // Fallback to primary with DPI
                var source = PresentationSource.FromVisual(this);
                var transform = source?.CompositionTarget?.TransformFromDevice ?? System.Windows.Media.Matrix.Identity;
                var bottomRight = transform.Transform(new System.Windows.Point(SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight));
                
                Left = 0;
                Top = 0;
                Width = bottomRight.X;
                Height = bottomRight.Y;
            }

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            Cursor = Cursors.None;
            WindowState = WindowState.Normal;
            Topmost = true;

            // Re-attach renderer and resume playback
            try
            {
                VideoView.MediaPlayer = _playbackService is PlaybackService svc ? svc.MediaPlayer : null;
                if (wasPlaying)
                {
                    await _playbackService.ResumeAsync();
                }
            }
            catch { }

            ShowOsd("Fullscreen");
        }
        else
        {
            // Detach before exit fullscreen
            try { VideoView.MediaPlayer = null; } catch { }

            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            ShowInTaskbar = true;
            Cursor = Cursors.Arrow;
            WindowState = WindowState.Normal;
            Width = 900;
            Height = 600;
            Topmost = false;
            
            // Re-attach and continue playing
            try
            {
                VideoView.MediaPlayer = _playbackService is PlaybackService svc ? svc.MediaPlayer : null;
            }
            catch { }

            PositionOnSelectedMonitor();
            ShowOsd("Windowed");
        }
    }

    public async Task LoadVideoAsync(string videoPath)
    {
        try
        {
            ErrorMessage.Text = string.Empty;
            _viewModel.CurrentVideoPath = videoPath;
            
            // Apply loop setting BEFORE playing video (so Media is created with correct option)
            if (_playbackService is SnowblindModPlayer.Infrastructure.Services.PlaybackService svc)
            {
                svc.SetLoopEnabled(_viewModel.LoopEnabled);
            }
            
            await _playbackService.PlayAsync(videoPath);
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
                
                // Also update OSD if visible
                Progress.Maximum = 100;
                Progress.Value = totalTime > 0 ? (currentTime * 100.0 / totalTime) : 0;
                TimeText.Text = $"{FormatTime(currentTime)} / {FormatTime(totalTime)}";
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
        Dispatcher.InvokeAsync(() =>
        {
            if (_playbackService.IsPlaying)
            {
                PlayPauseButton.Content = "?"; // Pause
            }
            else
            {
                PlayPauseButton.Content = "?"; // Play
            }
        });
    }

    private void PlaybackService_PlaybackPositionChanged(object? sender, EventArgs e)
    {
        // UI will be updated by the timer tick
    }

    private void PlaybackService_MediaEnded(object? sender, EventArgs e)
    {
        // Loop is now handled directly in PlaybackService EndReached
        // This only fires when loop is disabled or loop restart fails
    }

    private void PlaybackService_VolumeChanged(object? sender, EventArgs e)
    {
        Dispatcher.InvokeAsync(() =>
        {
            VolumeSlider.Value = _playbackService.VolumePercent;
            VolumeLabel.Text = $"{_playbackService.VolumePercent}%";
            VolText.Text = _playbackService.IsMuted ? "Muted" : $"Vol {_playbackService.VolumePercent}%";
        });
    }

    #endregion

    #region OSD System

    private void ShowOsd(string? status)
    {
        if (status != null)
              OsdLine1.Text = status;

        Osd.Visibility = Visibility.Visible;
        _osdHideTimer?.Stop();
        _osdHideTimer?.Start();
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

