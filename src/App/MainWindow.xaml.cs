using WorkspaceManager.App.ViewModels;

namespace WorkspaceManager.App;

public partial class MainWindow : System.Windows.Window
{
    private readonly AppSettings _appSettings;
    private readonly AppSettingsStore _settingsStore;
    private readonly DesktopIconService _desktopIconService;
    private readonly StartupRegistrationService _startupRegistrationService;
    private readonly MainWindowViewModel _viewModel;
    private GlobalHotkeyService? _globalHotkeyService;
    private bool _allowClose;

    public MainWindow(
        DesktopIconService desktopIconService,
        AppSettings appSettings,
        AppSettingsStore settingsStore,
        StartupRegistrationService startupRegistrationService)
    {
        InitializeComponent();
        _appSettings = appSettings;
        _settingsStore = settingsStore;
        _desktopIconService = desktopIconService;
        _startupRegistrationService = startupRegistrationService;
        _viewModel = new MainWindowViewModel();
        DataContext = _viewModel;
        StateChanged += OnStateChanged;
        Closing += OnClosing;
        LoadSettingsIntoView();
    }

    public void AttachHotkeyService(GlobalHotkeyService hotkeyService)
    {
        _globalHotkeyService = hotkeyService;
        SourceInitialized += OnSourceInitialized;
        Closed += OnClosed;
        _viewModel.SetHotkey(hotkeyService.DisplayText);
    }

    public void RefreshDesktopIconState()
    {
        try
        {
            var isVisible = _desktopIconService.IsVisible();
            _viewModel.SetDesktopIconState(isVisible);
        }
        catch (Exception ex)
        {
            _viewModel.SetStatus($"读取桌面状态失败：{ex.Message}");
        }
    }

    public void ShowFromTray()
    {
        Show();
        WindowState = System.Windows.WindowState.Normal;
        Activate();
        RefreshDesktopIconState();
        _viewModel.SetStatus("已从托盘恢复窗口。");
    }

    public void HideToTray(string message)
    {
        Hide();
        RefreshDesktopIconState();
        _viewModel.SetStatus(message);
    }

    public void RequestExit()
    {
        _allowClose = true;
        Close();
    }

    private async Task ToggleDesktopIconsAsync()
    {
        try
        {
            await _desktopIconService.ToggleAsync();
            RefreshDesktopIconState();
        }
        catch (Exception ex)
        {
            _viewModel.SetStatus($"切换桌面图标失败：{ex.Message}");
        }
    }

    private async void ToggleDesktopIcons_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        await ToggleDesktopIconsAsync();
    }

    private void RefreshState_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        RefreshDesktopIconState();
    }

    private void SaveSettings_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            _appSettings.LaunchAtStartup = _viewModel.LaunchAtStartupEnabled;
            _appSettings.StartMinimizedToTray = _viewModel.StartMinimizedToTrayEnabled;

            _startupRegistrationService.SetEnabled(_appSettings.LaunchAtStartup);
            _settingsStore.Save(_appSettings);

            _viewModel.SetStatus("设置已保存。");
        }
        catch (Exception ex)
        {
            _viewModel.SetStatus($"保存设置失败：{ex.Message}");
        }
    }

    private void ReloadSettings_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var latest = _settingsStore.Load();
        latest.LaunchAtStartup = _startupRegistrationService.IsEnabled();

        _appSettings.LaunchAtStartup = latest.LaunchAtStartup;
        _appSettings.StartMinimizedToTray = latest.StartMinimizedToTray;

        LoadSettingsIntoView();
        _viewModel.SetStatus("设置已重新加载。");
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        if (_globalHotkeyService is null)
        {
            return;
        }

        try
        {
            _globalHotkeyService.Register(this);
            _globalHotkeyService.HotkeyPressed += OnHotkeyPressed;
            _viewModel.SetStatus("托盘与全局快捷键已可用。");
        }
        catch (Exception ex)
        {
            _viewModel.SetStatus($"快捷键注册失败：{ex.Message}");
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (_globalHotkeyService is null)
        {
            return;
        }

        _globalHotkeyService.HotkeyPressed -= OnHotkeyPressed;
    }

    private async void OnHotkeyPressed(object? sender, EventArgs e)
    {
        await Dispatcher.InvokeAsync(ToggleDesktopIconsAsync);
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (WindowState != System.Windows.WindowState.Minimized)
        {
            return;
        }

        HideToTray("窗口已最小化到托盘。");
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_allowClose)
        {
            return;
        }

        e.Cancel = true;
        HideToTray("窗口已关闭到托盘，应用仍在后台运行。");
    }

    private void LoadSettingsIntoView()
    {
        _viewModel.SetLaunchAtStartup(_appSettings.LaunchAtStartup);
        _viewModel.SetStartMinimizedToTray(_appSettings.StartMinimizedToTray);
    }
}
