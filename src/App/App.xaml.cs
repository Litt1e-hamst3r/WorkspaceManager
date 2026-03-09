namespace WorkspaceManager.App;

public partial class App : System.Windows.Application
{
    private AppSettings? _settings;
    private AppSettingsStore? _settingsStore;
    private GlobalHotkeyService? _globalHotkeyService;
    private StartupRegistrationService? _startupRegistrationService;
    private TrayIconHost? _trayIconHost;

    public DesktopIconService DesktopIconService { get; } = new();

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        _settingsStore = new AppSettingsStore();
        _startupRegistrationService = new StartupRegistrationService();
        _settings = _settingsStore.Load();
        _settings.LaunchAtStartup = _startupRegistrationService.IsEnabled();

        var mainWindow = new MainWindow(
            DesktopIconService,
            _settings,
            _settingsStore,
            _startupRegistrationService);
        MainWindow = mainWindow;

        _globalHotkeyService = new GlobalHotkeyService();
        mainWindow.AttachHotkeyService(_globalHotkeyService);

        _trayIconHost = new TrayIconHost(mainWindow, DesktopIconService);
        _trayIconHost.Initialize();

        if (_settings.StartMinimizedToTray)
        {
            mainWindow.Show();
            mainWindow.HideToTray("已按设置启动到托盘。");
            return;
        }

        mainWindow.Show();
        mainWindow.RefreshDesktopIconState();
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        _globalHotkeyService?.Dispose();
        _trayIconHost?.Dispose();
        base.OnExit(e);
    }
}
