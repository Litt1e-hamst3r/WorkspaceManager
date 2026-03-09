using Xunit;
using WorkspaceManager.Domain.Models;

namespace WorkspaceManager.Domain.Tests;

public sealed class DesktopModeTests
{
    [Fact]
    public void Constructor_Assigns_Properties()
    {
        var mode = new DesktopMode(
            "work",
            "工作模式",
            true,
            "layout-work",
            true,
            new[] { "documents" },
            false);

        Assert.Equal("work", mode.Id);
        Assert.Equal("工作模式", mode.Name);
        Assert.True(mode.DesktopIconsVisible);
    }
}
