using System.Windows;
using System.Windows.Media;

namespace SnowblindModPlayer;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        StateChanged += (_, _) => UpdateCaptionButtons();
        Loaded += (_, _) => UpdateCaptionButtons();
    }

    private void TitleBar_OnMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Don't start a window drag when the user clicks on interactive controls (caption buttons).
        if (e.OriginalSource is DependencyObject d)
        {
            var current = d;
            while (current != null)
            {
                if (current is System.Windows.Controls.Button)
                    return;
                current = VisualTreeHelper.GetParent(current);
            }
        }

        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            return;
        }

        DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void MaxRestoreButton_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void CloseButton_Click(object sender, RoutedEventArgs e)
        => Close();

    private void UpdateCaptionButtons()
    {
        if (MaxRestoreButton == null)
            return;

        // Segoe Fluent Icons: Maximize E922, Restore E923
        MaxRestoreButton.Content = WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
        MaxRestoreButton.ToolTip = WindowState == WindowState.Maximized ? "Restore" : "Maximize";
    }
}
