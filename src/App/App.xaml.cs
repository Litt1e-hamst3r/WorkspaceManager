namespace WorkspaceManager.App;

public partial class App : System.Windows.Application
{
    private DesktopLayoutService? _desktopLayoutService;
    private DesktopLayoutStore? _desktopLayoutStore;
    private DesktopLayoutPreviewService? _desktopLayoutPreviewService;
    private AppSettings? _settings;
    private AppSettingsStore? _settingsStore;
    private GlobalHotkeyService? _globalHotkeyService;
    private StartupRegistrationService? _startupRegistrationService;
    private TaskbarService? _taskbarService;
    private TrayIconHost? _trayIconHost;

    public DesktopIconService DesktopIconService { get; } = new();

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        _settingsStore = new AppSettingsStore();
        _startupRegistrationService = new StartupRegistrationService();
        _desktopLayoutStore = new DesktopLayoutStore();
        _desktopLayoutPreviewService = new DesktopLayoutPreviewService();
        _taskbarService = new TaskbarService();
        _desktopLayoutService = new DesktopLayoutService(_desktopLayoutStore, _desktopLayoutPreviewService);
        _settings = _settingsStore.Load();
        _settings.LaunchAtStartup = _startupRegistrationService.IsEnabled();

        var mainWindow = new MainWindow(
            DesktopIconService,
            _taskbarService,
            _desktopLayoutService,
            _settings,
            _settingsStore,
            _startupRegistrationService);
        MainWindow = mainWindow;

        _globalHotkeyService = new GlobalHotkeyService();
        mainWindow.AttachHotkeyService(_globalHotkeyService);

        _trayIconHost = new TrayIconHost(mainWindow, DesktopIconService, _taskbarService);
        _trayIconHost.Initialize();

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
        _globalHotkeyService?.Dispose();
        _trayIconHost?.Dispose();
        base.OnExit(e);
    }
}
