namespace WorkspaceManager.Infrastructure.Configuration;

public sealed class WallpaperSourceSetting
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string RequestUrl { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;
}
