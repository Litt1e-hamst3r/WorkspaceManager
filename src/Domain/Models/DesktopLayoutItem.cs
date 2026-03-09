namespace WorkspaceManager.Domain.Models;

public sealed record DesktopLayoutItem(
    string Path,
    string DisplayName,
    int PositionX,
    int PositionY,
    string ItemType);
