using System.Globalization;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using SnowblindModPlayer.Core.Services;

namespace SnowblindModPlayer.Infrastructure.Services;

public class PlaybackService : IPlaybackService
{
    private readonly LibVLC _libVLC;
    private readonly MediaPlayer _mediaPlayer;
    private long _currentPositionMs;
    private long _durationMs;
    private string _currentMediaPath = string.Empty;
    private bool _mediaLoaded;
    private Media? _currentMedia;
    private bool _loopEnabled;
    private Action<Func<Task>>? _uiDispatcher;

    public long CurrentPositionMs => _currentPositionMs;
    public long DurationMs => _durationMs;
    public bool IsPlaying => _mediaPlayer.IsPlaying;
    public bool IsMuted => _mediaPlayer.Mute;
    public int VolumePercent => _mediaPlayer.Volume;

    public event EventHandler? PlayingStateChanged;
    public event EventHandler? PlaybackPositionChanged;
    public event EventHandler? VolumeChanged;
    public event EventHandler? MediaEnded;
    public event EventHandler? MediaEndReached;  // Exposed for external loop handling

    public LibVLCSharp.Shared.MediaPlayer MediaPlayer => _mediaPlayer;

    public PlaybackService()
    {
        LibVLCSharp.Shared.Core.Initialize();
        
        // VLC flags based on proven working PowerShell script
        _libVLC = new LibVLCSharp.Shared.LibVLC(
            "--repeat",                     // Repeat playlist
            "--loop",                       // Loop each file
            "--autoscale",                  // Auto-scale to window
            "--no-osd",                     // No on-screen display
            "--no-video-title-show",        // No video title overlay
            "--no-sub-autodetect-file",     // No subtitle auto-loading
            "--no-video-deco",              // No window decorations
            "--quiet",                      // Suppress output
            "--disable-screensaver"         // Keep screen active
        );
        
        _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);

        _mediaPlayer.LengthChanged += (s, e) =>
        {
            _durationMs = e.Length;
            PlaybackPositionChanged?.Invoke(this, EventArgs.Empty);
        };
        _mediaPlayer.PositionChanged += (s, e) =>
        {
            _currentPositionMs = (long)(_durationMs * e.Position);
            PlaybackPositionChanged?.Invoke(this, EventArgs.Empty);
        };
        
        // EndReached: Both --repeat and --loop should handle looping, but fallback just in case
        _mediaPlayer.EndReached += (s, e) =>
        {
            if (_loopEnabled && _mediaLoaded && !string.IsNullOrEmpty(_currentMediaPath))
            {
                try
                {
                    _mediaPlayer.Position = 0.0f;
                    System.Diagnostics.Debug.WriteLine("Loop: Position reset to 0");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Loop restart failed: {ex}");
                }
            }
            MediaEndReached?.Invoke(this, EventArgs.Empty);
        };
        
        _mediaPlayer.Playing += (s, e) => PlayingStateChanged?.Invoke(this, EventArgs.Empty);
        _mediaPlayer.Paused += (s, e) => PlayingStateChanged?.Invoke(this, EventArgs.Empty);
        _mediaPlayer.Stopped += (s, e) => PlayingStateChanged?.Invoke(this, EventArgs.Empty);
        _mediaPlayer.VolumeChanged += (s, e) => VolumeChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetWindowHandle(IntPtr windowHandle) { }
    public IntPtr GetWindowHandle() => IntPtr.Zero;

    public void SetUIDispatcher(Action<Func<Task>> dispatcher)
    {
        _uiDispatcher = dispatcher;
    }

    public Task PlayAsync(string videoPath)
    {
        if (string.IsNullOrWhiteSpace(videoPath))
            return Task.CompletedTask;

        try
        {
            LoadMedia(videoPath);
            _mediaPlayer.Play();
            
            // FORCE STRETCH after play - these work directly on MediaPlayer
            _mediaPlayer.AspectRatio = null;  // null = fill window
            _mediaPlayer.Scale = 0;           // 0 = fit to window
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Play failed: {ex}");
        }

        return Task.CompletedTask;
    }

    private void LoadMedia(string videoPath)
    {
        if (_mediaPlayer.IsPlaying)
        {
            _mediaPlayer.Stop();
        }

        _currentMedia?.Dispose();
        _currentMediaPath = videoPath;
        _currentMedia = CreateConfiguredMedia(videoPath);
        _mediaPlayer.Media = _currentMedia;
        _mediaLoaded = true;
        _currentPositionMs = 0;
    }

    private Media CreateConfiguredMedia(string videoPath)
    {
        var media = new Media(_libVLC, videoPath, FromType.FromPath);

        // Disable all OSD and UI elements
        media.AddOption(":no-osd");
        media.AddOption(":no-video-title-show");
        media.AddOption(":quiet");
        media.AddOption(":disable-lua");
        
        return media;
    }

    public void SetLoopEnabled(bool enabled)
    {
        _loopEnabled = enabled;
        System.Diagnostics.Debug.WriteLine($"Loop set to: {(enabled ? "ON" : "OFF")}");
    }

    public Task PauseAsync()
    {
        try { _mediaPlayer.SetPause(true); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Pause failed: {ex}"); }
        return Task.CompletedTask;
    }

    public Task ResumeAsync()
    {
        try
        {
            if (_mediaLoaded)
            {
                if (_mediaPlayer.State == LibVLCSharp.Shared.VLCState.Paused)
                    _mediaPlayer.SetPause(false);
                else
                    _mediaPlayer.Play();
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Resume failed: {ex}"); }
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        try { _mediaPlayer.Stop(); _currentPositionMs = 0; } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Stop failed: {ex}"); }
        return Task.CompletedTask;
    }

    public Task SeekAsync(long positionMs)
    {
        try
        {
            if (_durationMs > 0)
            {
                var pos = Math.Clamp((float)positionMs / _durationMs, 0f, 1f);
                _mediaPlayer.Position = pos;
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Seek failed: {ex}"); }
        return Task.CompletedTask;
    }

    public Task SetVolumeAsync(int volumePercent)
    {
        try { _mediaPlayer.Volume = Math.Clamp(volumePercent, 0, 100); VolumeChanged?.Invoke(this, EventArgs.Empty); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"SetVolume failed: {ex}"); }
        return Task.CompletedTask;
    }

    public Task SetMuteAsync(bool muted)
    {
        try { _mediaPlayer.Mute = muted; VolumeChanged?.Invoke(this, EventArgs.Empty); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"SetMute failed: {ex}"); }
        return Task.CompletedTask;
    }
}
