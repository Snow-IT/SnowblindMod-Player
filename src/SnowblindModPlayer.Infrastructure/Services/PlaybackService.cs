using SnowblindModPlayer.Core.Services;

namespace SnowblindModPlayer.Infrastructure.Services;

public class PlaybackService : IPlaybackService
{
    public Task PlayAsync(string videoPath)
    {
        // TODO: Implement LibVLC playback
        return Task.CompletedTask;
    }

    public Task PauseAsync()
    {
        // TODO: Pause playback
        return Task.CompletedTask;
    }

    public Task ResumeAsync()
    {
        // TODO: Resume playback
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        // TODO: Stop playback
        return Task.CompletedTask;
    }

    public Task SeekAsync(long positionMs)
    {
        // TODO: Seek to position
        return Task.CompletedTask;
    }

    public Task SetVolumeAsync(int volumePercent)
    {
        // TODO: Set volume level (0-100)
        return Task.CompletedTask;
    }

    public Task SetMuteAsync(bool muted)
    {
        // TODO: Toggle mute
        return Task.CompletedTask;
    }
}
