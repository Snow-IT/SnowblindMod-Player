using System.Windows.Controls;
using SnowblindModPlayer.ViewModels;

namespace SnowblindModPlayer.Views;

public partial class VideosView : UserControl
{
    public VideosView(VideosViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
