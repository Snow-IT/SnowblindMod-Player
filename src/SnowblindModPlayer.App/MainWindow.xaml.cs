using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SnowblindModPlayer.Core.Services;

namespace SnowblindModPlayer;

public class BannerEntry
{
    public string Message { get; set; } = string.Empty;
    public string Background { get; set; } = "#FF2D2D30";
    public Guid Id { get; set; } = Guid.NewGuid();
}

public partial class MainWindow : Window
{
    private readonly ObservableCollection<BannerEntry> _banners = new();

    public MainWindow()
    {
        InitializeComponent();
        // Explicitly set icon for taskbar/title
        Icon = BitmapFrame.Create(new Uri("pack://application:,,,/Assets/Icon.ico", UriKind.Absolute));
        BannerHost.ItemsSource = _banners;
        StateChanged += (_, _) => UpdateCaptionButtons();
        Loaded += (_, _) => UpdateCaptionButtons();
    }

    public void ShowBanner(string message, NotificationType type, int durationMs)
    {
        Dispatcher.Invoke(() =>
        {
            var entry = new BannerEntry
            {
                Message = message,
                Background = type switch
                {
                    NotificationType.Success => "#FF1D6F42",
                    NotificationType.Warning => "#FF8E562E",
                    NotificationType.Error => "#FF7A1D1D",
                    _ => "#FF2D2D30"
                }
            };
            _banners.Add(entry);

            // Limit to 3 visible; remove oldest if needed
            while (_banners.Count > 3)
            {
                _banners.RemoveAt(0);
            }

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(durationMs) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                _banners.Remove(entry);
            };
            timer.Start();
        });
    }

    private void TitleBar_OnMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Don't start a window drag when the user clicks on interactive controls (caption buttons).
        if (e.OriginalSource is DependencyObject d)
        {
            var current = d;
            while (current != null)
            {
                if (current is Button)
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
