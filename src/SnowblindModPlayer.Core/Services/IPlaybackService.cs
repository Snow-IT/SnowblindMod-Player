namespace SnowblindModPlayer.Core.Services;

public interface IPlaybackService
{
    Task PlayAsync(string videoPath);
    Task PauseAsync();
    Task ResumeAsync();
    Task StopAsync();
    Task SeekAsync(long positionMs);
    Task SetVolumeAsync(int volumePercent);
    Task SetMuteAsync(bool muted);

    // Expose properties for UI state tracking
    long CurrentPositionMs { get; }
    long DurationMs { get; }
    bool IsPlaying { get; }
    bool IsMuted { get; }
    int VolumePercent { get; }

    // Get the native window handle for rendering
    IntPtr GetWindowHandle();
    void SetWindowHandle(IntPtr windowHandle);

    // Events for state changes
    event EventHandler? PlayingStateChanged;
    event EventHandler? PlaybackPositionChanged;
    event EventHandler? VolumeChanged;
    event EventHandler? MediaEnded;
}
