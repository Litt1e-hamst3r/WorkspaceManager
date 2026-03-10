namespace WorkspaceManager.Infrastructure.Configuration;

public sealed class AppSettings
{
    public const string DefaultDesktopToggleHotkey = "Ctrl+Shift+D";
    public const string DefaultShowMainWindowHotkey = "Ctrl+Alt+W";
    public const string DefaultModeIdValue = "default";

    public bool LaunchAtStartup { get; set; }

    public bool StartMinimizedToTray { get; set; }

    public bool MinimizeToTrayOnMinimize { get; set; } = true;

    public bool CloseToTrayOnClose { get; set; } = true;

    public string DesktopToggleHotkey { get; set; } = DefaultDesktopToggleHotkey;

    public string ShowMainWindowHotkey { get; set; } = DefaultShowMainWindowHotkey;

    public string DefaultModeId { get; set; } = DefaultModeIdValue;
}
