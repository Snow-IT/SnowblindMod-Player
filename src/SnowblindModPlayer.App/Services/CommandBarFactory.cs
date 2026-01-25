using SnowblindModPlayer.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace SnowblindModPlayer.Services;

public static class CommandBarFactory
{
    // Segoe Fluent Icons Glyphs
    private const string IconAdd = "\uE710";        // Add
    private const string IconDelete = "\uE74D";     // Delete
    private const string IconStar = "\uE735";       // FavoriteStar
    private const string IconStarFill = "\uE734";   // FavoriteStarFill
    private const string IconRefresh = "\uE72C";    // Refresh
    private const string IconFolder = "\uE8B7";     // FolderOpen

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

        var addBtn = new Button
        {
            Content = IconAdd,
            ToolTip = "Add video",
            Style = (Style)Application.Current.FindResource("CommandButton"),
            Margin = new Thickness(0, 0, 8, 0),
            Command = vm.ImportCommand
        };

        var delBtn = new Button
        {
            Content = IconDelete,
            ToolTip = "Remove selected video",
            Style = (Style)Application.Current.FindResource("IconButton"),
            Margin = new Thickness(0, 0, 8, 0),
            Command = vm.RemoveCommand
        };

        var starBtn = new Button
        {
            Content = IconStar,
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
        // Logs commands will be wired once LogsViewModel is implemented
        var panel = new StackPanel { Orientation = Orientation.Horizontal };

        var refreshBtn = new Button
        {
            Content = IconRefresh,
            ToolTip = "Refresh logs",
            Style = (Style)Application.Current.FindResource("IconButton"),
            Margin = new Thickness(0, 0, 8, 0)
        };

        var deleteBtn = new Button
        {
            Content = IconDelete,
            ToolTip = "Delete selected log",
            Style = (Style)Application.Current.FindResource("IconButton"),
            Margin = new Thickness(0, 0, 8, 0),
            IsEnabled = false
        };

        var folderBtn = new Button
        {
            Content = IconFolder,
            ToolTip = "Open logs folder",
            Style = (Style)Application.Current.FindResource("IconButton")
        };

        panel.Children.Add(refreshBtn);
        panel.Children.Add(deleteBtn);
        panel.Children.Add(folderBtn);

        return panel;
    }
}
