using System.Net.Http;
using WorkspaceManager.Application.Layouts;
using WorkspaceManager.Application.Modes;
using WorkspaceManager.Application.Wallpapers;
using WorkspaceManager.Infrastructure.Configuration;
using WorkspaceManager.Infrastructure.Layouts;
using WorkspaceManager.Infrastructure.Modes;
using WorkspaceManager.Infrastructure.Wallpapers;
using WorkspaceManager.Interop.Desktop;
using WorkspaceManager.Interop.Hotkeys;
using WorkspaceManager.Interop.Layouts;
using WorkspaceManager.Interop.Startup;
using WorkspaceManager.UI.Services;
using WorkspaceManager.UI.Shell;

namespace WorkspaceManager.App;

public partial class App : System.Windows.Application
{
    private DesktopLayoutService? _desktopLayoutService;
    private DesktopLayoutStore? _desktopLayoutStore;
    private DesktopLayoutPreviewService? _desktopLayoutPreviewService;
    private ModeService? _modeService;
    private ModeStore? _modeStore;
    private AppSettings? _settings;
    private AppSettingsStore? _settingsStore;
    private GlobalHotkeyService? _desktopToggleHotkeyService;
    private GlobalHotkeyService? _showMainWindowHotkeyService;
    private DisplaySettingsWatcher? _displaySettingsWatcher;
    private DesktopLayoutProtectionService? _desktopLayoutProtectionService;
    private StartupRegistrationService? _startupRegistrationService;
    private TaskbarService? _taskbarService;
    private TrayIconHost? _trayIconHost;
    private WallpaperAutoRotationService? _wallpaperAutoRotationService;
    private HttpClient? _httpClient;

    public DesktopIconService DesktopIconService { get; } = new();

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        _settingsStore = new AppSettingsStore();
        _startupRegistrationService = new StartupRegistrationService();
        _desktopLayoutStore = new DesktopLayoutStore();
        _desktopLayoutPreviewService = new DesktopLayoutPreviewService();
        _settings = _settingsStore.Load();
        var desktopLayoutInteropService = new DesktopLayoutInteropService();
        _displaySettingsWatcher = new DisplaySettingsWatcher();
        _httpClient = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = true
        })
        {
            Timeout = TimeSpan.FromSeconds(8)
        };
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("WorkspaceManager/0.1");
        var wallpaperImageStore = new WallpaperImageStore(_settings.FavoriteWallpaperSaveDirectory);
        var wallpaperRotationService = new WallpaperRotationService(
            _httpClient,
            wallpaperImageStore,
            new DesktopWallpaperService());
        _wallpaperAutoRotationService = new WallpaperAutoRotationService();
        var mainWindowViewDataBuilder = new MainWindowViewDataBuilder(new LayoutPreviewImageLoader());
        _modeStore = new ModeStore();
        _taskbarService = new TaskbarService();
        _desktopLayoutService = new DesktopLayoutService(_desktopLayoutStore, _desktopLayoutPreviewService, desktopLayoutInteropService);
        _desktopLayoutProtectionService = new DesktopLayoutProtectionService(_desktopLayoutService, _displaySettingsWatcher, Dispatcher);
        _modeService = new ModeService(_modeStore, DesktopIconService, _taskbarService, _desktopLayoutService);
        _settings.LaunchAtStartup = _startupRegistrationService.IsEnabled();
        try
        {
            _settings.DesktopToggleHotkey = GlobalHotkeyService.Normalize(_settings.DesktopToggleHotkey);
            _settings.ShowMainWindowHotkey = GlobalHotkeyService.Normalize(_settings.ShowMainWindowHotkey);
        }
        catch
        {
            _settings.DesktopToggleHotkey = AppSettings.DefaultDesktopToggleHotkey;
            _settings.ShowMainWindowHotkey = AppSettings.DefaultShowMainWindowHotkey;
            _settingsStore.Save(_settings);
        }

        if (string.IsNullOrWhiteSpace(_settings.DefaultModeId))
        {
            _settings.DefaultModeId = AppSettings.DefaultModeIdValue;
        }

        var mainWindow = new MainWindow(
            DesktopIconService,
            _taskbarService,
            _desktopLayoutService,
            _modeService,
            wallpaperRotationService,
            _wallpaperAutoRotationService,
            _settings,
            _settingsStore,
            _startupRegistrationService,
            mainWindowViewDataBuilder);
        MainWindow = mainWindow;

        _desktopToggleHotkeyService = new GlobalHotkeyService(_settings.DesktopToggleHotkey);
        _showMainWindowHotkeyService = new GlobalHotkeyService(_settings.ShowMainWindowHotkey);
        mainWindow.AttachHotkeyServices(_desktopToggleHotkeyService, _showMainWindowHotkeyService);

        _trayIconHost = new TrayIconHost(mainWindow, DesktopIconService, _taskbarService);
        _trayIconHost.Initialize();
        _desktopLayoutProtectionService.Start();

        mainWindow.PrimeWallpaperCache();
        mainWindow.ApplyConfiguredDefaultMode();
        mainWindow.ScheduleStartupWallpaperRefresh();

        if (_settings.StartMinimizedToTray)
        {
            mainWindow.Show();
            mainWindow.HideToTray("已按设置启动到托盘。");
            return;
        }

        mainWindow.Show();
        mainWindow.RefreshDesktopIconState();
        mainWindow.RefreshTaskbarState();
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        _desktopToggleHotkeyService?.Dispose();
        _showMainWindowHotkeyService?.Dispose();
        _desktopLayoutProtectionService?.Dispose();
        _wallpaperAutoRotationService?.Dispose();
        _trayIconHost?.Dispose();
        _httpClient?.Dispose();
        base.OnExit(e);
    }
}
