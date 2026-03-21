namespace WorkspaceManager.Application.Wallpapers;

public sealed class WallpaperChangeResult
{
    public string SourceId { get; init; } = string.Empty;

    public string SourceName { get; init; } = string.Empty;

    public string SavedPath { get; init; } = string.Empty;
}
