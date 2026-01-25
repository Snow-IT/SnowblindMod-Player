using SnowblindModPlayer.UI.MVVM;

namespace SnowblindModPlayer.UI.ViewModels;

public class PlayerWindowViewModel : ViewModelBase
{
    private string _currentVideoPath = string.Empty;
    private bool _loopEnabled = true;
    private bool _muteEnabled = false;
    private int _volumePercent = 50;
    private bool _fullscreenOnStart = true;
    private string _scalingMode = "Fill"; // "Fill" or "KeepAspect"

    public string CurrentVideoPath
    {
        get => _currentVideoPath;
        set => SetProperty(ref _currentVideoPath, value);
    }

    public bool LoopEnabled
    {
        get => _loopEnabled;
        set => SetProperty(ref _loopEnabled, value);
    }

    public bool MuteEnabled
    {
        get => _muteEnabled;
        set => SetProperty(ref _muteEnabled, value);
    }

    public int VolumePercent
    {
        get => _volumePercent;
        set => SetProperty(ref _volumePercent, value);
    }

    public bool FullscreenOnStart
    {
        get => _fullscreenOnStart;
        set => SetProperty(ref _fullscreenOnStart, value);
    }

    public string ScalingMode
    {
        get => _scalingMode;
        set => SetProperty(ref _scalingMode, value);
    }
}
