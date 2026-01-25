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
}
