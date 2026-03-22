namespace WorkspaceManager.Infrastructure.Configuration;

public sealed class AppSettings
{
    public const string DefaultDesktopToggleHotkey = "Ctrl+Shift+D";
    public const string DefaultShowMainWindowHotkey = "Ctrl+Alt+W";
    public const string DefaultModeIdValue = "default";
    public const int DefaultWallpaperRotationIntervalMinutes = 30;

    public bool LaunchAtStartup { get; set; }

    public bool StartMinimizedToTray { get; set; }

    public bool MinimizeToTrayOnMinimize { get; set; } = true;

    public bool CloseToTrayOnClose { get; set; } = true;

    public string DesktopToggleHotkey { get; set; } = DefaultDesktopToggleHotkey;

    public string ShowMainWindowHotkey { get; set; } = DefaultShowMainWindowHotkey;

    public string DefaultModeId { get; set; } = DefaultModeIdValue;

    public bool WallpaperChangeOnStartup { get; set; }

    public bool WallpaperAutoRotateEnabled { get; set; }

    public int WallpaperRotationIntervalMinutes { get; set; } = DefaultWallpaperRotationIntervalMinutes;

    public List<WallpaperSourceSetting> WallpaperSources { get; set; } = CreateDefaultWallpaperSources();

    public static bool IsBuiltInWallpaperSourceId(string? id)
    {
        return !string.IsNullOrWhiteSpace(id)
            && CreateDefaultWallpaperSources().Any(source => string.Equals(source.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public static List<WallpaperSourceSetting> CreateDefaultWallpaperSources()
    {
        return
        [
            new()
            {
                Id = "alcy-pc",
                Name = "Alcy PC",
                RequestUrl = "https://t.alcy.cc/pc",
                Kind = WallpaperSourceKind.RemoteUrl,
                Enabled = true
            },
            new()
            {
                Id = "loliapi-pc",
                Name = "LoliAPI 横图",
                RequestUrl = "https://www.loliapi.com/acg/pc/",
                Kind = WallpaperSourceKind.RemoteUrl,
                Enabled = true
            },
            new()
            {
                Id = "yppp",
                Name = "YPPP 随机图",
                RequestUrl = "https://api.yppp.net/api.php",
                Kind = WallpaperSourceKind.RemoteUrl,
                Enabled = true
            },
            new()
            {
                Id = "mtyqx-random",
                Name = "MTYQX 随机图",
                RequestUrl = "https://api.mtyqx.cn/api/random.php",
                Kind = WallpaperSourceKind.RemoteUrl,
                Enabled = true
            },
            new()
            {
                Id = "alcy-moe",
                Name = "Alcy Moe",
                RequestUrl = "https://t.alcy.cc/moe",
                Kind = WallpaperSourceKind.RemoteUrl,
                Enabled = true
            },
            new()
            {
                Id = "tomys-acgimg",
                Name = "Tomys ACG",
                RequestUrl = "https://api.tomys.top/api/acgimg",
                Kind = WallpaperSourceKind.RemoteUrl,
                Enabled = true
            },
            new()
            {
                Id = "zichen-acg",
                Name = "Zichen ACG",
                RequestUrl = "https://app.zichen.zone/api/acg/api.php",
                Kind = WallpaperSourceKind.RemoteUrl,
                Enabled = true
            },
            new()
            {
                Id = "lolicon-setu",
                Name = "Lolicon Setu",
                RequestUrl = "https://api.lolicon.app/setu/v2",
                Kind = WallpaperSourceKind.RemoteUrl,
                Enabled = true
            }
        ];
    }
}
