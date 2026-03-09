namespace WorkspaceManager.Infrastructure.Configuration;

public sealed class AppSettings
{
    public bool LaunchAtStartup { get; set; }

    public bool StartMinimizedToTray { get; set; } = true;

    public string? DefaultModeId { get; set; }

    public bool RememberLastMode { get; set; } = true;

    public bool AutoOrganizeEnabled { get; set; }
}
