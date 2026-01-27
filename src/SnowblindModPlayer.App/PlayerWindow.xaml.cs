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
        }

        // Subscribe to playback events
        _playbackService.PlayingStateChanged += PlaybackService_PlayingStateChanged;
        _playbackService.VolumeChanged += PlaybackService_VolumeChanged;
        
        // Load settings
        LoadSettingsAsync();

        // Position window on selected monitor (or primary if none selected)
        PositionOnSelectedMonitor();

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
        var volume = _settingsService.GetVolume();
        var muted = _settingsService.GetMuted();
        _viewModel.ScalingMode = _settingsService.GetScalingMode();

        // Apply volume and mute
        await _playbackService.SetVolumeAsync(volume);
        await _playbackService.SetMuteAsync(muted);
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
                if (string.IsNullOrEmpty(_viewModel.CurrentVideoPath))
                {
                    System.Diagnostics.Debug.WriteLine("No video loaded");
                }
                else
                {
                    await _playbackService.PlayAsync(_viewModel.CurrentVideoPath);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Playback error: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine($"Seek error: {ex.Message}");
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
    }

    private void ToggleLoopAsync()
    {
        _viewModel.LoopEnabled = !_viewModel.LoopEnabled;
        if (_playbackService is PlaybackService svc)
        {
            svc.SetLoopEnabled(_viewModel.LoopEnabled);
        }
    }

    private async void ToggleMuteAsync()
    {
        bool newMuteState = !_playbackService.IsMuted;
        await _playbackService.SetMuteAsync(newMuteState);
    }

    private async void ToggleFullscreenAsync()
    {
        _isFullscreen = !_isFullscreen;
        if (_isFullscreen)
        {
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
        }
        else
        {
            // Exit fullscreen - restore window
            Cursor = Cursors.Arrow;
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            ShowInTaskbar = true;
            Topmost = false;
            WindowState = WindowState.Normal;
            Width = 900;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }

    public async Task LoadVideoAsync(string videoPath)
    {
        try
        {
            _viewModel.CurrentVideoPath = videoPath;
            
            // Apply loop setting BEFORE playing video (handled by VLC --loop flag)
            if (_playbackService is SnowblindModPlayer.Infrastructure.Services.PlaybackService svc)
            {
                svc.SetLoopEnabled(_viewModel.LoopEnabled);
            }
            
            await _playbackService.PlayAsync(videoPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load video: {ex.Message}");
        }
    }

    private void ClosePlayer()
    {
        _ = _playbackService.StopAsync();
        Close();
    }

    #endregion

    #region Playback Service Events

    private void PlaybackService_PlayingStateChanged(object? sender, EventArgs e)
    {
        // No UI update needed (no overlays in Option B)
    }

    private void PlaybackService_VolumeChanged(object? sender, EventArgs e)
    {
        // No UI update needed (no overlays in Option B)
    }

    #endregion
}

