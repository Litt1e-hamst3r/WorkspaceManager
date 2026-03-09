namespace WorkspaceManager.App.ViewModels;

public sealed class LayoutSummaryViewModel
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public int ItemCount { get; init; }

    public string DisplayText { get; init; } = string.Empty;
}
