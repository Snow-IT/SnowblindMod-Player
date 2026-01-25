using SnowblindModPlayer.Infrastructure.Services;
using Xunit;

namespace SnowblindModPlayer.Tests;

public class AppDataPathServiceTests
{
    [Fact]
    public void GetAppDataRoot_ReturnsValidPath()
    {
        var service = new AppDataPathService();
        var root = service.GetAppDataRoot();
        
        Assert.NotEmpty(root);
        Assert.Contains("SnowblindModPlayer", root);
    }

    [Fact]
    public void GetSettingsFilePath_ReturnsJsonPath()
    {
        var service = new AppDataPathService();
        var path = service.GetSettingsFilePath();
        
        Assert.EndsWith("settings.json", path);
    }

    [Fact]
    public void GetLibraryDbPath_ReturnsDbPath()
    {
        var service = new AppDataPathService();
        var path = service.GetLibraryDbPath();
        
        Assert.EndsWith("library.db", path);
    }
}
