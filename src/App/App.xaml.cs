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
        _modeStore = new ModeStore();
        _taskbarService = new TaskbarService();
        _desktopLayoutService = new DesktopLayoutService(_desktopLayoutStore, _desktopLayoutPreviewService);
        _modeService = new ModeService(_modeStore, DesktopIconService, _taskbarService, _desktopLayoutService);
        _settings = _settingsStore.Load();
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
            _settings,
            _settingsStore,
            _startupRegistrationService);
        MainWindow = mainWindow;

        _desktopToggleHotkeyService = new GlobalHotkeyService(_settings.DesktopToggleHotkey);
        _showMainWindowHotkeyService = new GlobalHotkeyService(_settings.ShowMainWindowHotkey);
        mainWindow.AttachHotkeyServices(_desktopToggleHotkeyService, _showMainWindowHotkeyService);

        _trayIconHost = new TrayIconHost(mainWindow, DesktopIconService, _taskbarService);
        _trayIconHost.Initialize();

        mainWindow.ApplyConfiguredDefaultMode();

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
        _trayIconHost?.Dispose();
        base.OnExit(e);
    }
}
