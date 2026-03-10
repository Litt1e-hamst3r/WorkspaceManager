namespace WorkspaceManager.Domain.Layouts;

public sealed class DesktopLayoutItem
{
    public string DisplayName { get; set; } = string.Empty;

    public int PositionX { get; set; }

    public int PositionY { get; set; }
}
