using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SnowblindModPlayer.ViewModels;

namespace SnowblindModPlayer.Views;

public partial class VideosView : UserControl
{
    public VideosView(VideosViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        // Load videos when view is first loaded
        Loaded += async (s, e) =>
        {
            if (viewModel != null)
            {
                await viewModel.LoadVideosAsync();
            }
        };
    }
}

