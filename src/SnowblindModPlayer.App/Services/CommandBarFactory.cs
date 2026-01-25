using SnowblindModPlayer.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace SnowblindModPlayer.Services;

public static class CommandBarFactory
{
    public static object? CreateForPage(string page, object? view)
    {
        return page switch
        {
            "Videos" => CreateVideos(view as SnowblindModPlayer.Views.VideosView),
            "Logs" => CreateLogs(view as SnowblindModPlayer.Views.LogsView),
            _ => null,
        };
    }

    private static UIElement? CreateVideos(SnowblindModPlayer.Views.VideosView? view)
    {
        if (view?.DataContext is not VideosViewModel vm)
            return null;

        var panel = new StackPanel { Orientation = Orientation.Horizontal };

        var iconFont = new System.Windows.Media.FontFamily("Segoe MDL2 Assets");

        var addBtn = new Button
        {
            Content = new TextBlock { Text = "\uE710", FontFamily = iconFont, FontSize = 16 },
            ToolTip = "Add video",
            Style = (Style)Application.Current.FindResource("CommandButton"),
            Margin = new Thickness(0, 0, 8, 0),
            Command = vm.ImportCommand
        };

        var delBtn = new Button
        {
            Content = new TextBlock { Text = "\uE74D", FontFamily = iconFont, FontSize = 16 },
            ToolTip = "Remove selected video",
            Style = (Style)Application.Current.FindResource("IconButton"),
            Margin = new Thickness(0, 0, 8, 0),
            Command = vm.RemoveCommand
        };

        var starBtn = new Button
        {
            Content = new TextBlock { Text = "\uE734", FontFamily = iconFont, FontSize = 16 },
            ToolTip = "Set as default video",
            Style = (Style)Application.Current.FindResource("IconButton"),
            Command = vm.SetDefaultCommand
        };

        panel.Children.Add(addBtn);
        panel.Children.Add(delBtn);
        panel.Children.Add(starBtn);

        return panel;
    }

    private static UIElement? CreateLogs(SnowblindModPlayer.Views.LogsView? view)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        var iconFont = new System.Windows.Media.FontFamily("Segoe MDL2 Assets");

        var refreshBtn = new Button
        {
            Content = new TextBlock { Text = "\uE72C", FontFamily = iconFont, FontSize = 16 },
            ToolTip = "Refresh logs",
            Style = (Style)Application.Current.FindResource("IconButton"),
            Margin = new Thickness(0, 0, 8, 0)
        };

        var deleteBtn = new Button
        {
            Content = new TextBlock { Text = "\uE74D", FontFamily = iconFont, FontSize = 16 },
            ToolTip = "Delete selected log",
            Style = (Style)Application.Current.FindResource("IconButton"),
            Margin = new Thickness(0, 0, 8, 0),
            IsEnabled = false
        };

        var folderBtn = new Button
        {
            Content = new TextBlock { Text = "\uE838", FontFamily = iconFont, FontSize = 16 },
            ToolTip = "Open logs folder",
            Style = (Style)Application.Current.FindResource("IconButton")
        };

        panel.Children.Add(refreshBtn);
        panel.Children.Add(deleteBtn);
        panel.Children.Add(folderBtn);

        return panel;
    }
}
