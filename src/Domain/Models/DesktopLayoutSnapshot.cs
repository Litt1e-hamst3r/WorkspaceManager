namespace WorkspaceManager.Domain.Models;

public sealed record DesktopLayoutSnapshot(
    string Id,
    string Name,
    int ResolutionWidth,
    int ResolutionHeight,
    DateTimeOffset CreatedAt,
    IReadOnlyList<DesktopLayoutItem> Items);
