using System.Windows.Media;

namespace WorkspaceManager.UI.ViewModels;

public sealed class LayoutSummaryViewModel
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string PreviewImagePath { get; init; } = string.Empty;

    public ImageSource? ThumbnailImage { get; init; }

    public ImageSource? PreviewImage { get; init; }

    public int ItemCount { get; init; }

    public string DisplayText { get; init; } = string.Empty;
}
