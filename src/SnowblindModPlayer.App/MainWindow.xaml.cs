using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SnowblindModPlayer.Core.Services;

namespace SnowblindModPlayer;

public class BannerEntry
{
    public string Message { get; set; } = string.Empty;
    public Brush Background { get; set; } = new SolidColorBrush(Color.FromRgb(45, 45, 48));
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
            // Get theme-aware color based on type
            var resourceKey = type switch
            {
                NotificationType.Success => "Brush.Success",
                NotificationType.Warning => "Brush.Warning",
                NotificationType.Error => "Brush.Error",
                _ => "Brush.Info"
            };

            var backgroundBrush = Application.Current.Resources[resourceKey] as System.Windows.Media.Brush 
                ?? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray);

            var entry = new BannerEntry
            {
                Message = message,
                Background = backgroundBrush
            };
            _banners.Add(entry);

            // Limit to 3 visible; remove oldest if needed
            while (_banners.Count > 3)
            {
                _banners.RemoveAt(0);
            }

            // Auto-dismiss with fade-out animation
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(durationMs) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                RemoveBannerWithAnimation(entry);
            };
            timer.Start();
        });
    }

    private void RemoveBannerWithAnimation(BannerEntry entry)
    {
        // Find the visual element (Border) for this banner
        var container = BannerHost;
        var itemIndex = _banners.IndexOf(entry);
        if (itemIndex < 0) return;

        // Get the UI element (ItemsControl generates Borders in ItemTemplate)
        var ui = (UIElement?)BannerHost.ItemContainerGenerator.ContainerFromIndex(itemIndex);
        if (ui is not Border border)
        {
            // Fallback: just remove
            _banners.Remove(entry);
            return;
        }

        // Animate fade-out + slide-up
        var storyboard = new System.Windows.Media.Animation.Storyboard();

        // Fade out animation (opacity 1 ? 0 in 300ms)
        var fadeOut = new System.Windows.Media.Animation.DoubleAnimation
        {
            From = 1.0,
            To = 0.0,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
        };
        System.Windows.Media.Animation.Storyboard.SetTarget(fadeOut, border);
        System.Windows.Media.Animation.Storyboard.SetTargetProperty(fadeOut, new PropertyPath(OpacityProperty));
        storyboard.Children.Add(fadeOut);

        // Slide-up animation (Y: 0 ? -50 in 300ms)
        var slideUp = new System.Windows.Media.Animation.DoubleAnimation
        {
            From = 0.0,
            To = -50.0,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
        };
        var transform = border.RenderTransform as System.Windows.Media.TranslateTransform ?? new System.Windows.Media.TranslateTransform();
        border.RenderTransform = transform;
        System.Windows.Media.Animation.Storyboard.SetTarget(slideUp, border.RenderTransform);
        System.Windows.Media.Animation.Storyboard.SetTargetProperty(slideUp, new PropertyPath(System.Windows.Media.TranslateTransform.YProperty));
        storyboard.Children.Add(slideUp);

        // On animation complete, remove from collection
        storyboard.Completed += (s, e) =>
        {
            _banners.Remove(entry);
            System.Diagnostics.Debug.WriteLine($"?? Banner animated and removed: {entry.Id}");
        };

        storyboard.Begin();
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
