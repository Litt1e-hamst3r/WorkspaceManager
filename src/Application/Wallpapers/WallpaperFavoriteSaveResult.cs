namespace WorkspaceManager.Application.Wallpapers;

public sealed class WallpaperFavoriteSaveResult
{
    public string SavedPath { get; init; } = string.Empty;

    public string DirectoryPath { get; init; } = string.Empty;

    public bool AlreadyExists { get; init; }
}
