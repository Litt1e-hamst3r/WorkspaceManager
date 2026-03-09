using WorkspaceManager.App.ViewModels;

namespace WorkspaceManager.App;

public partial class MainWindow : System.Windows.Window
{
    private readonly AppSettings _appSettings;
    private readonly DesktopLayoutService _desktopLayoutService;
    private readonly AppSettingsStore _settingsStore;
    private readonly DesktopIconService _desktopIconService;
    private readonly StartupRegistrationService _startupRegistrationService;
    private readonly MainWindowViewModel _viewModel;
    private GlobalHotkeyService? _globalHotkeyService;
    private bool _allowClose;

    public MainWindow(
        DesktopIconService desktopIconService,
        DesktopLayoutService desktopLayoutService,
        AppSettings appSettings,
        AppSettingsStore settingsStore,
        StartupRegistrationService startupRegistrationService)
    {
        InitializeComponent();
        _appSettings = appSettings;
        _desktopLayoutService = desktopLayoutService;
        _settingsStore = settingsStore;
        _desktopIconService = desktopIconService;
        _startupRegistrationService = startupRegistrationService;
        _viewModel = new MainWindowViewModel();
        DataContext = _viewModel;

        StateChanged += OnStateChanged;
        Closing += OnClosing;

        LoadSettingsIntoView();
        LoadLayoutsIntoView();
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

    private void SaveLayout_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            var snapshot = _desktopLayoutService.Capture(_viewModel.LayoutNameInput);
            _desktopLayoutService.Save(snapshot);
            LoadLayoutsIntoView();
            _viewModel.SetLayoutNameInput(string.Empty);
            _viewModel.SetStatus($"布局“{snapshot.Name}”已保存。");
        }
        catch (Exception ex)
        {
            _viewModel.SetStatus($"保存布局失败：{ex.Message}");
        }
    }

    private void ReloadLayouts_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        LoadLayoutsIntoView();
        _viewModel.SetStatus("布局列表已刷新。");
    }

    private void RestoreLayout_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_viewModel.SelectedLayout is null)
        {
            _viewModel.SetStatus("请先选择一个布局。");
            return;
        }

        try
        {
            _desktopLayoutService.Restore(_viewModel.SelectedLayout.Id);
            RefreshDesktopIconState();
            _viewModel.SetStatus($"已恢复布局“{_viewModel.SelectedLayout.Name}”。");
        }
        catch (Exception ex)
        {
            _viewModel.SetStatus($"恢复布局失败：{ex.Message}");
        }
    }

    private void DeleteLayout_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_viewModel.SelectedLayout is null)
        {
            _viewModel.SetStatus("请先选择一个布局。");
            return;
        }

        try
        {
            var deletedName = _viewModel.SelectedLayout.Name;
            _desktopLayoutService.Delete(_viewModel.SelectedLayout.Id);
            LoadLayoutsIntoView();
            _viewModel.SetStatus($"已删除布局“{deletedName}”。");
        }
        catch (Exception ex)
        {
            _viewModel.SetStatus($"删除布局失败：{ex.Message}");
        }
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
            _viewModel.SetStatus("托盘、全局快捷键和布局功能已可用。");
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

    private void LoadLayoutsIntoView()
    {
        var layouts = _desktopLayoutService
            .GetSavedLayouts()
            .Select(layout => new LayoutSummaryViewModel
            {
                Id = layout.Id,
                Name = layout.Name,
                ItemCount = layout.Items.Count,
                DisplayText = $"{layout.Name} · {layout.Items.Count} 个图标 · {layout.CreatedAt:yyyy-MM-dd HH:mm}"
            });

        _viewModel.SetLayouts(layouts);
    }
}
