namespace WorkspaceManager.App;

public sealed class DesktopMode
{
    public const string DefaultModeId = "default";
    public const string WorkModeId = "work";
    public const string PresentationModeId = "presentation";

    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool DesktopIconsVisible { get; set; }

    public bool TaskbarVisible { get; set; } = true;

    public string LayoutId { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;

    public bool IsBuiltIn =>
        string.Equals(Id, DefaultModeId, StringComparison.OrdinalIgnoreCase)
        || string.Equals(Id, WorkModeId, StringComparison.OrdinalIgnoreCase)
        || string.Equals(Id, PresentationModeId, StringComparison.OrdinalIgnoreCase);
}
