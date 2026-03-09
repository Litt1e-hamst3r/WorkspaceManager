namespace WorkspaceManager.App;

public sealed class DesktopLayoutSnapshot
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int ResolutionWidth { get; set; }

    public int ResolutionHeight { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<DesktopLayoutItem> Items { get; set; } = [];
}
