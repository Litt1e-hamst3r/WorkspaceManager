using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WorkspaceManager.App.ViewModels;

namespace WorkspaceManager.App;

public partial class MainWindow : System.Windows.Window
{
    private readonly AppSettings _appSettings;
    private readonly DesktopLayoutService _desktopLayoutService;
    private readonly ModeService _modeService;
    private readonly AppSettingsStore _settingsStore;
    private readonly DesktopIconService _desktopIconService;
    private readonly TaskbarService _taskbarService;
    private readonly StartupRegistrationService _startupRegistrationService;
    private readonly MainWindowViewModel _viewModel;
    private GlobalHotkeyService? _desktopToggleHotkeyService;
    private GlobalHotkeyService? _showMainWindowHotkeyService;
    private string? _currentModeId;
    private bool _allowClose;

    public MainWindow(
        DesktopIconService desktopIconService,
        TaskbarService taskbarService,
        DesktopLayoutService desktopLayoutService,
        ModeService modeService,
        AppSettings appSettings,
        AppSettingsStore settingsStore,
        StartupRegistrationService startupRegistrationService)
    {
        InitializeComponent();
        _appSettings = appSettings;
        _desktopLayoutService = desktopLayoutService;
        _modeService = modeService;
        _settingsStore = settingsStore;
        _desktopIconService = desktopIconService;
        _taskbarService = taskbarService;
        _startupRegistrationService = startupRegistrationService;
        _viewModel = new MainWindowViewModel();
        DataContext = _viewModel;

        StateChanged += OnStateChanged;
        Closing += OnClosing;

        LoadSettingsIntoView();
        LoadLayoutsIntoView();
        LoadModesIntoView();
    }

    public void AttachHotkeyServices(
        GlobalHotkeyService desktopToggleHotkeyService,
        GlobalHotkeyService showMainWindowHotkeyService)
    {
        _desktopToggleHotkeyService = desktopToggleHotkeyService;
        _showMainWindowHotkeyService = showMainWindowHotkeyService;
        SourceInitialized += OnSourceInitialized;
        Closed += OnClosed;
        _viewModel.SetDesktopToggleHotkeyInput(desktopToggleHotkeyService.DisplayText);
        _viewModel.SetShowMainWindowHotkeyInput(showMainWindowHotkeyService.DisplayText);
        _viewModel.SetHotkeys(desktopToggleHotkeyService.DisplayText, showMainWindowHotkeyService.DisplayText);
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

    public void RefreshTaskbarState()
    {
        try
        {
            var isVisible = _taskbarService.IsVisible();
            _viewModel.SetTaskbarState(isVisible);
        }
        catch (Exception ex)
        {
            _viewModel.SetStatus($"读取任务栏状态失败：{ex.Message}");
        }
    }

    public void ShowFromTray()
    {
        Show();
        WindowState = System.Windows.WindowState.Normal;
        Activate();
        RefreshDesktopIconState();
        RefreshTaskbarState();
        _viewModel.SetStatus("已从托盘恢复窗口。");
    }

    public void HideToTray(string message)
    {
        Hide();
        RefreshDesktopIconState();
        RefreshTaskbarState();
        _viewModel.SetStatus(message);
    }

    public void RequestExit()
    {
        _allowClose = true;
        Close();
    }

    public void ApplyConfiguredDefaultMode()
    {
        if (string.IsNullOrWhiteSpace(_appSettings.DefaultModeId))
        {
            return;
        }

        try
        {
            ApplyModeAsync(_appSettings.DefaultModeId, "已应用默认模式“{0}”。")
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            _viewModel.SetStatus($"应用默认模式失败：{ex.Message}");
        }
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

    private async Task SetDesktopIconsVisibleAsync(bool visible)
    {
        try
        {
            await _desktopIconService.SetVisibleAsync(visible);
            RefreshDesktopIconState();
            _viewModel.SetStatus($"桌面图标已{(visible ? "显示" : "隐藏")}。");
        }
        catch (Exception ex)
        {
            _viewModel.SetStatus($"{(visible ? "显示" : "隐藏")}桌面图标失败：{ex.Message}");
        }
    }

    private async Task SetTaskbarVisibleAsync(bool visible)
    {
        try
        {
            await _taskbarService.SetVisibleAsync(visible);
            RefreshTaskbarState();
            _viewModel.SetStatus($"任务栏已{(visible ? "显示" : "隐藏")}。");
        }
        catch (Exception ex)
        {
            _viewModel.SetStatus($"{(visible ? "显示" : "隐藏")}任务栏失败：{ex.Message}");
        }
    }

    private async void ToggleDesktopIcons_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        await ToggleDesktopIconsAsync();
    }

    private async void ShowDesktopIcons_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        await SetDesktopIconsVisibleAsync(true);
    }

    private async void HideDesktopIcons_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        await SetDesktopIconsVisibleAsync(false);
    }

    private void RefreshState_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        RefreshDesktopIconState();
        RefreshTaskbarState();
    }

    private async void ToggleTaskbar_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            await _taskbarService.ToggleAsync();
            RefreshTaskbarState();
            _viewModel.SetStatus("任务栏状态已切换。");
        }
        catch (Exception ex)
        {
            _viewModel.SetStatus($"切换任务栏失败：{ex.Message}");
        }
    }

    private async void ShowTaskbar_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        await SetTaskbarVisibleAsync(true);
    }

    private async void HideTaskbar_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        await SetTaskbarVisibleAsync(false);
    }

    private void SaveSettings_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var previousDesktopToggleHotkey = _desktopToggleHotkeyService?.DisplayText ?? _appSettings.DesktopToggleHotkey;
        var previousShowMainWindowHotkey = _showMainWindowHotkeyService?.DisplayText ?? _appSettings.ShowMainWindowHotkey;

        try
        {
            var normalizedDesktopToggleHotkey = GlobalHotkeyService.Normalize(_viewModel.DesktopToggleHotkeyInput);
            var normalizedShowMainWindowHotkey = GlobalHotkeyService.Normalize(_viewModel.ShowMainWindowHotkeyInput);

            if (string.Equals(normalizedDesktopToggleHotkey, normalizedShowMainWindowHotkey, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("两个快捷键不能设置成相同组合。");
            }

            _desktopToggleHotkeyService?.UpdateHotkey(normalizedDesktopToggleHotkey);

            try
            {
                _showMainWindowHotkeyService?.UpdateHotkey(normalizedShowMainWindowHotkey);
            }
            catch
            {
                if (_desktopToggleHotkeyService is not null
                    && !string.Equals(previousDesktopToggleHotkey, normalizedDesktopToggleHotkey, StringComparison.Ordinal))
                {
                    _desktopToggleHotkeyService.UpdateHotkey(previousDesktopToggleHotkey);
                }

                throw;
            }

            _appSettings.LaunchAtStartup = _viewModel.LaunchAtStartupEnabled;
            _appSettings.StartMinimizedToTray = _viewModel.StartMinimizedToTrayEnabled;
            _appSettings.MinimizeToTrayOnMinimize = _viewModel.MinimizeToTrayOnMinimizeEnabled;
            _appSettings.CloseToTrayOnClose = _viewModel.CloseToTrayOnCloseEnabled;
            _appSettings.DesktopToggleHotkey = normalizedDesktopToggleHotkey;
            _appSettings.ShowMainWindowHotkey = normalizedShowMainWindowHotkey;
            _appSettings.DefaultModeId = string.IsNullOrWhiteSpace(_viewModel.DefaultModeId)
                ? AppSettings.DefaultModeIdValue
                : _viewModel.DefaultModeId;

            _startupRegistrationService.SetEnabled(_appSettings.LaunchAtStartup);
            _settingsStore.Save(_appSettings);
            _viewModel.SetDesktopToggleHotkeyInput(normalizedDesktopToggleHotkey);
            _viewModel.SetShowMainWindowHotkeyInput(normalizedShowMainWindowHotkey);
            _viewModel.SetHotkeys(normalizedDesktopToggleHotkey, normalizedShowMainWindowHotkey);
            _viewModel.SetDefaultModeName(FindModeName(_appSettings.DefaultModeId));
            LoadModesIntoView();

            _viewModel.SetStatus("设置已保存。");
        }
        catch (Exception ex)
        {
            if (_desktopToggleHotkeyService is not null)
            {
                _viewModel.SetDesktopToggleHotkeyInput(_desktopToggleHotkeyService.DisplayText);
            }

            if (_showMainWindowHotkeyService is not null)
            {
                _viewModel.SetShowMainWindowHotkeyInput(_showMainWindowHotkeyService.DisplayText);
            }

            _viewModel.SetHotkeys(
                _desktopToggleHotkeyService?.DisplayText ?? previousDesktopToggleHotkey,
                _showMainWindowHotkeyService?.DisplayText ?? previousShowMainWindowHotkey);
            _viewModel.SetStatus($"保存设置失败：{ex.Message}");
        }
    }

    private async void ApplyMode_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_viewModel.SelectedMode is null)
        {
            _viewModel.SetStatus("请先选择一个模式。");
            return;
        }

        await ApplyModeAsync(_viewModel.SelectedMode.Id, "已切换到模式“{0}”。");
    }

    private void SetDefaultMode_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_viewModel.SelectedMode is null)
        {
            _viewModel.SetStatus("请先选择一个模式。");
            return;
        }

        _appSettings.DefaultModeId = _viewModel.SelectedMode.Id;
        _settingsStore.Save(_appSettings);
        _viewModel.SetDefaultModeId(_appSettings.DefaultModeId);
        _viewModel.SetDefaultModeName(_viewModel.SelectedMode.Name);
        LoadModesIntoView();
        _viewModel.SetStatus($"已将“{_viewModel.SelectedMode.Name}”设为默认模式。");
    }

    private void SaveModeLayoutBinding_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_viewModel.SelectedMode is null)
        {
            _viewModel.SetStatus("请先选择一个模式。");
            return;
        }

        try
        {
            var layoutId = string.IsNullOrWhiteSpace(_viewModel.SelectedModeLayoutId)
                ? string.Empty
                : _viewModel.SelectedModeLayoutId;

            var updatedMode = _modeService.UpdateLayoutBinding(_viewModel.SelectedMode.Id, layoutId);
            LoadModesIntoView(updatedMode.Id);
            _viewModel.SetStatus(string.IsNullOrWhiteSpace(updatedMode.LayoutId)
                ? $"已清除“{updatedMode.Name}”的布局绑定。"
                : $"已保存“{updatedMode.Name}”的布局绑定。");
        }
        catch (Exception ex)
        {
            _viewModel.SetStatus($"保存模式布局失败：{ex.Message}");
        }
    }

    private void AddCustomMode_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        OpenCreateModeEditor();
    }

    private void EditCustomMode_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_viewModel.SelectedMode is null)
        {
            _viewModel.SetStatus("请先选择一个模式。");
            return;
        }

        if (_viewModel.SelectedMode.IsBuiltIn)
        {
            _viewModel.SetStatus("预设模式暂不支持编辑。");
            return;
        }

        var editor = new ModeEditorWindow("编辑自定义模式", _viewModel.ModeLayoutOptions, _viewModel.SelectedMode)
        {
            Owner = this
        };

        if (editor.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var updatedMode = _modeService.UpdateCustomMode(
                _viewModel.SelectedMode.Id,
                editor.ModeName,
                editor.Description,
                editor.DesktopIconsVisible,
                editor.TaskbarVisible,
                editor.LayoutId);

            if (string.Equals(_appSettings.DefaultModeId, updatedMode.Id, StringComparison.OrdinalIgnoreCase))
            {
                _viewModel.SetDefaultModeName(updatedMode.Name);
            }

            if (string.Equals(_currentModeId, updatedMode.Id, StringComparison.OrdinalIgnoreCase))
            {
                _viewModel.SetCurrentMode(updatedMode.Name);
            }

            LoadModesIntoView(updatedMode.Id);
            _viewModel.SetStatus($"已更新自定义模式“{updatedMode.Name}”。");
        }
        catch (Exception ex)
        {
            _viewModel.SetStatus($"更新自定义模式失败：{ex.Message}");
        }
    }

    private void DeleteCustomMode_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_viewModel.SelectedMode is null)
        {
            _viewModel.SetStatus("请先选择一个模式。");
            return;
        }

        if (_viewModel.SelectedMode.IsBuiltIn)
        {
            _viewModel.SetStatus("预设模式暂不支持删除。");
            return;
        }

        var mode = _viewModel.SelectedMode;
        var result = System.Windows.MessageBox.Show(
            this,
            $"确定删除自定义模式“{mode.Name}”吗？",
            "删除自定义模式",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            _modeService.DeleteCustomMode(mode.Id);
            HandleDeletedModeReferences(mode.Id);
            LoadModesIntoView();
            _viewModel.SetStatus($"已删除自定义模式“{mode.Name}”。");
        }
        catch (Exception ex)
        {
            _viewModel.SetStatus($"删除自定义模式失败：{ex.Message}");
        }
    }

    private void HotkeyCapture_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBox textBox)
        {
            return;
        }

        if (!textBox.IsKeyboardFocusWithin)
        {
            e.Handled = true;
            textBox.Focus();
        }
    }

    private void HotkeyCapture_GotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBox textBox)
        {
            return;
        }

        textBox.SelectAll();
        _viewModel.SetStatus(BuildHotkeyCaptureFocusMessage(textBox.Tag as string));
    }

    private void HotkeyCapture_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBox textBox)
        {
            return;
        }

        if (e.Key == System.Windows.Input.Key.Tab)
        {
            return;
        }

        if (e.Key == System.Windows.Input.Key.Escape
            && System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.None)
        {
            System.Windows.Input.Keyboard.ClearFocus();
            _viewModel.SetStatus("已取消本次快捷键录入。");
            e.Handled = true;
            return;
        }

        var key = e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key;
        if (IsModifierOnlyKey(key))
        {
            _viewModel.SetStatus("请继续按主按键，例如 Ctrl+Shift+D。");
            e.Handled = true;
            return;
        }

        try
        {
            var capturedHotkey = GlobalHotkeyService.Normalize(System.Windows.Input.Keyboard.Modifiers, key);
            ApplyCapturedHotkey(textBox.Tag as string, capturedHotkey);
            textBox.SelectAll();
            _viewModel.SetStatus($"已捕获快捷键：{capturedHotkey}。点击“保存设置”后生效。");
        }
        catch (Exception ex)
        {
            _viewModel.SetStatus($"快捷键录入失败：{ex.Message}");
        }

        e.Handled = true;
    }

    private void HotkeyCapture_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        e.Handled = true;
    }

    private void ReloadSettings_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var latest = _settingsStore.Load();
        latest.LaunchAtStartup = _startupRegistrationService.IsEnabled();

        _appSettings.LaunchAtStartup = latest.LaunchAtStartup;
        _appSettings.StartMinimizedToTray = latest.StartMinimizedToTray;
        _appSettings.MinimizeToTrayOnMinimize = latest.MinimizeToTrayOnMinimize;
        _appSettings.CloseToTrayOnClose = latest.CloseToTrayOnClose;
        _appSettings.DesktopToggleHotkey = string.IsNullOrWhiteSpace(latest.DesktopToggleHotkey)
            ? AppSettings.DefaultDesktopToggleHotkey
            : latest.DesktopToggleHotkey;
        _appSettings.ShowMainWindowHotkey = string.IsNullOrWhiteSpace(latest.ShowMainWindowHotkey)
            ? AppSettings.DefaultShowMainWindowHotkey
            : latest.ShowMainWindowHotkey;
        _appSettings.DefaultModeId = string.IsNullOrWhiteSpace(latest.DefaultModeId)
            ? AppSettings.DefaultModeIdValue
            : latest.DefaultModeId;

        if (_desktopToggleHotkeyService is not null)
        {
            try
            {
                _desktopToggleHotkeyService.UpdateHotkey(_appSettings.DesktopToggleHotkey);
                _appSettings.DesktopToggleHotkey = _desktopToggleHotkeyService.DisplayText;
            }
            catch (Exception ex)
            {
                _appSettings.DesktopToggleHotkey = _desktopToggleHotkeyService.DisplayText;
                LoadSettingsIntoView();
                _viewModel.SetHotkeys(
                    _desktopToggleHotkeyService.DisplayText,
                    _showMainWindowHotkeyService?.DisplayText ?? _appSettings.ShowMainWindowHotkey);
                _viewModel.SetStatus($"重新加载设置时快捷键未更新：{ex.Message}");
                return;
            }
        }

        if (_showMainWindowHotkeyService is not null)
        {
            try
            {
                _showMainWindowHotkeyService.UpdateHotkey(_appSettings.ShowMainWindowHotkey);
                _appSettings.ShowMainWindowHotkey = _showMainWindowHotkeyService.DisplayText;
            }
            catch (Exception ex)
            {
                _appSettings.ShowMainWindowHotkey = _showMainWindowHotkeyService.DisplayText;
                LoadSettingsIntoView();
                _viewModel.SetHotkeys(
                    _desktopToggleHotkeyService?.DisplayText ?? _appSettings.DesktopToggleHotkey,
                    _showMainWindowHotkeyService.DisplayText);
                _viewModel.SetStatus($"重新加载设置时快捷键未更新：{ex.Message}");
                return;
            }
        }

        LoadSettingsIntoView();
        _viewModel.SetHotkeys(_appSettings.DesktopToggleHotkey, _appSettings.ShowMainWindowHotkey);
        LoadModesIntoView();
        _viewModel.SetStatus("设置已重新加载。");
    }

    private async void SaveLayout_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            var snapshot = _desktopLayoutService.Capture(_viewModel.LayoutNameInput);
            await SaveLayoutWithPreviewAsync(snapshot);
            LoadLayoutsIntoView();
            _viewModel.SetLayoutNameInput(string.Empty);
            _viewModel.SetStatus(string.IsNullOrWhiteSpace(snapshot.PreviewImageFileName)
                ? $"布局“{snapshot.Name}”已保存。"
                : $"布局“{snapshot.Name}”已保存，并生成预览图。");
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
            RefreshTaskbarState();
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

    private void SelectedLayoutPreview_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        OpenPreviewWindow(_viewModel.SelectedLayout);
    }

    private void LayoutItemPreview_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not System.Windows.FrameworkElement element || element.DataContext is not LayoutSummaryViewModel layout)
        {
            return;
        }

        _viewModel.SelectedLayout = layout;
        OpenPreviewWindow(layout);
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        if (_desktopToggleHotkeyService is null || _showMainWindowHotkeyService is null)
        {
            return;
        }

        try
        {
            _desktopToggleHotkeyService.Register(this);
            _desktopToggleHotkeyService.HotkeyPressed += OnDesktopToggleHotkeyPressed;
            _showMainWindowHotkeyService.Register(this);
            _showMainWindowHotkeyService.HotkeyPressed += OnShowMainWindowHotkeyPressed;
            _viewModel.SetStatus("托盘、全局快捷键和布局功能已可用。");
        }
        catch (Exception ex)
        {
            _viewModel.SetStatus($"快捷键注册失败：{ex.Message}");
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (_desktopToggleHotkeyService is not null)
        {
            _desktopToggleHotkeyService.HotkeyPressed -= OnDesktopToggleHotkeyPressed;
        }

        if (_showMainWindowHotkeyService is not null)
        {
            _showMainWindowHotkeyService.HotkeyPressed -= OnShowMainWindowHotkeyPressed;
        }
    }

    private async void OnDesktopToggleHotkeyPressed(object? sender, EventArgs e)
    {
        await Dispatcher.InvokeAsync(ToggleDesktopIconsAsync);
    }

    private async void OnShowMainWindowHotkeyPressed(object? sender, EventArgs e)
    {
        await Dispatcher.InvokeAsync(ShowFromTray);
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (WindowState != System.Windows.WindowState.Minimized || !_appSettings.MinimizeToTrayOnMinimize)
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

        if (!_appSettings.CloseToTrayOnClose)
        {
            _allowClose = true;
            return;
        }

        e.Cancel = true;
        HideToTray("窗口已关闭到托盘，应用仍在后台运行。");
    }

    private void LoadSettingsIntoView()
    {
        _viewModel.SetLaunchAtStartup(_appSettings.LaunchAtStartup);
        _viewModel.SetStartMinimizedToTray(_appSettings.StartMinimizedToTray);
        _viewModel.SetMinimizeToTrayOnMinimize(_appSettings.MinimizeToTrayOnMinimize);
        _viewModel.SetCloseToTrayOnClose(_appSettings.CloseToTrayOnClose);
        _viewModel.SetDefaultModeId(string.IsNullOrWhiteSpace(_appSettings.DefaultModeId)
            ? AppSettings.DefaultModeIdValue
            : _appSettings.DefaultModeId);
        _viewModel.SetDesktopToggleHotkeyInput(string.IsNullOrWhiteSpace(_appSettings.DesktopToggleHotkey)
            ? AppSettings.DefaultDesktopToggleHotkey
            : _appSettings.DesktopToggleHotkey);
        _viewModel.SetShowMainWindowHotkeyInput(string.IsNullOrWhiteSpace(_appSettings.ShowMainWindowHotkey)
            ? AppSettings.DefaultShowMainWindowHotkey
            : _appSettings.ShowMainWindowHotkey);
        _viewModel.SetDefaultModeName(FindModeName(_viewModel.DefaultModeId));
    }

    private void LoadLayoutsIntoView()
    {
        var layouts = _desktopLayoutService
            .GetSavedLayouts()
            .Select(layout =>
            {
                var previewImagePath = _desktopLayoutService.GetPreviewPath(layout) ?? string.Empty;
                return new LayoutSummaryViewModel
                {
                    Id = layout.Id,
                    Name = layout.Name,
                    PreviewImagePath = previewImagePath,
                    ThumbnailImage = LoadPreviewImage(previewImagePath, 320),
                    PreviewImage = LoadPreviewImage(previewImagePath, 960),
                    ItemCount = layout.Items.Count,
                    DisplayText = BuildLayoutDisplayText(layout)
                };
            });

        _viewModel.SetLayouts(layouts);
        _viewModel.SetModeLayoutOptions(BuildModeLayoutOptions());
        if (_viewModel.SelectedMode is not null)
        {
            _viewModel.SetSelectedModeLayoutId(_viewModel.SelectedMode.LayoutId);
        }
        LoadModesIntoView(_viewModel.SelectedMode?.Id);
    }

    private void LoadModesIntoView(string? selectedModeId = null)
    {
        var layoutsById = _desktopLayoutService
            .GetSavedLayouts()
            .ToDictionary(layout => layout.Id, layout => layout.Name, StringComparer.OrdinalIgnoreCase);

        var modes = _modeService
            .GetModes()
            .Select(mode => new DesktopModeViewModel
            {
                Id = mode.Id,
                Name = mode.Name,
                Description = mode.Description,
                DesktopIconsVisible = mode.DesktopIconsVisible,
                TaskbarVisible = mode.TaskbarVisible,
                LayoutId = mode.LayoutId,
                LayoutName = ResolveLayoutName(mode.LayoutId, layoutsById),
                StateSummary = BuildModeStateSummary(mode, layoutsById),
                IsDefault = string.Equals(_appSettings.DefaultModeId, mode.Id, StringComparison.OrdinalIgnoreCase),
                IsActive = string.Equals(_currentModeId, mode.Id, StringComparison.OrdinalIgnoreCase),
                IsBuiltIn = mode.IsBuiltIn
            });

        _viewModel.SetModes(modes);
        if (!string.IsNullOrWhiteSpace(selectedModeId))
        {
            _viewModel.SelectedMode = _viewModel.Modes.FirstOrDefault(mode => string.Equals(mode.Id, selectedModeId, StringComparison.OrdinalIgnoreCase))
                ?? _viewModel.SelectedMode;
        }

        _viewModel.SetModeLayoutOptions(BuildModeLayoutOptions());
        _viewModel.SetModeOptions(BuildModeOptions());
        _viewModel.SetSelectedModeLayoutId(_viewModel.SelectedMode?.LayoutId);
        _viewModel.SetCurrentMode(FindModeName(_currentModeId));
        _viewModel.SetDefaultModeName(FindModeName(_appSettings.DefaultModeId));
    }

    private async Task SaveLayoutWithPreviewAsync(DesktopLayoutSnapshot snapshot)
    {
        var previousWindowState = WindowState;
        var wasVisible = IsVisible;

        try
        {
            if (wasVisible)
            {
                Hide();
                await Task.Delay(150);
            }

            _desktopLayoutService.Save(snapshot);
        }
        finally
        {
            if (wasVisible)
            {
                Show();
                WindowState = previousWindowState == System.Windows.WindowState.Minimized
                    ? System.Windows.WindowState.Normal
                    : previousWindowState;
                Activate();
            }
        }
    }

    private static ImageSource? LoadPreviewImage(string previewImagePath, int? targetPixelWidth = null)
    {
        if (string.IsNullOrWhiteSpace(previewImagePath) || !File.Exists(previewImagePath))
        {
            return null;
        }

        var bytes = File.ReadAllBytes(previewImagePath);
        using var stream = new MemoryStream(bytes);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames.FirstOrDefault();
        if (frame is null)
        {
            return null;
        }

        if (!targetPixelWidth.HasValue || frame.PixelWidth <= targetPixelWidth.Value)
        {
            frame.Freeze();
            return frame;
        }

        var scale = targetPixelWidth.Value / (double)frame.PixelWidth;
        var transformed = new TransformedBitmap(frame, new System.Windows.Media.ScaleTransform(scale, scale));
        transformed.Freeze();
        return transformed;
    }

    private static string BuildLayoutDisplayText(DesktopLayoutSnapshot layout)
    {
        var parts = new List<string>
        {
            layout.Name,
            $"{layout.Items.Count} 个图标"
        };

        parts.Add(layout.CreatedAt.ToString("yyyy-MM-dd HH:mm"));
        return string.Join(" · ", parts);
    }

    private async Task ApplyModeAsync(string modeId, string statusFormat)
    {
        try
        {
            var mode = await _modeService.SwitchAsync(modeId);
            _currentModeId = mode.Id;
            RefreshDesktopIconState();
            RefreshTaskbarState();
            LoadModesIntoView(mode.Id);
            _viewModel.SetStatus(string.Format(statusFormat, mode.Name));
        }
        catch (Exception ex)
        {
            _viewModel.SetStatus($"切换模式失败：{ex.Message}");
        }
    }

    private IEnumerable<ModeLayoutOptionViewModel> BuildModeLayoutOptions()
    {
        yield return new ModeLayoutOptionViewModel
        {
            Id = string.Empty,
            Name = "不恢复布局"
        };

        foreach (var layout in _desktopLayoutService.GetSavedLayouts())
        {
            yield return new ModeLayoutOptionViewModel
            {
                Id = layout.Id,
                Name = layout.Name
            };
        }
    }

    private IEnumerable<ModeOptionViewModel> BuildModeOptions()
    {
        foreach (var mode in _modeService.GetModes())
        {
            yield return new ModeOptionViewModel
            {
                Id = mode.Id,
                Name = mode.Name
            };
        }
    }

    private string? FindModeName(string? modeId)
    {
        return _modeService
            .GetModes()
            .FirstOrDefault(mode => string.Equals(mode.Id, modeId, StringComparison.OrdinalIgnoreCase))
            ?.Name;
    }

    private static string ResolveLayoutName(string? layoutId, IReadOnlyDictionary<string, string> layoutsById)
    {
        if (string.IsNullOrWhiteSpace(layoutId))
        {
            return "未绑定布局";
        }

        return layoutsById.TryGetValue(layoutId, out var name)
            ? name
            : "布局已丢失";
    }

    private static string BuildModeStateSummary(DesktopMode mode, IReadOnlyDictionary<string, string> layoutsById)
    {
        var parts = new List<string>
        {
            mode.DesktopIconsVisible ? "图标显示" : "图标隐藏",
            mode.TaskbarVisible ? "任务栏显示" : "任务栏隐藏",
            $"布局 {ResolveLayoutName(mode.LayoutId, layoutsById)}"
        };

        return string.Join(" · ", parts);
    }

    private void ApplyCapturedHotkey(string? target, string hotkey)
    {
        switch (target)
        {
            case "DesktopToggle":
                _viewModel.SetDesktopToggleHotkeyInput(hotkey);
                break;
            case "ShowMainWindow":
                _viewModel.SetShowMainWindowHotkeyInput(hotkey);
                break;
        }
    }

    private static bool IsModifierOnlyKey(System.Windows.Input.Key key)
    {
        return key is System.Windows.Input.Key.LeftCtrl
            or System.Windows.Input.Key.RightCtrl
            or System.Windows.Input.Key.LeftAlt
            or System.Windows.Input.Key.RightAlt
            or System.Windows.Input.Key.LeftShift
            or System.Windows.Input.Key.RightShift
            or System.Windows.Input.Key.LWin
            or System.Windows.Input.Key.RWin;
    }

    private static string BuildHotkeyCaptureFocusMessage(string? target)
    {
        return target switch
        {
            "DesktopToggle" => "已进入桌面图标快捷键录入，直接按组合键。",
            "ShowMainWindow" => "已进入主窗口快捷键录入，直接按组合键。",
            _ => "已进入快捷键录入，直接按组合键。"
        };
    }

    private void OpenPreviewWindow(LayoutSummaryViewModel? layout)
    {
        if (layout is null)
        {
            _viewModel.SetStatus("请先选择一个布局。");
            return;
        }

        var previewImage = LoadPreviewImage(layout.PreviewImagePath);
        if (previewImage is null)
        {
            _viewModel.SetStatus("该布局暂无预览图，请重新保存一次。");
            return;
        }

        var previewWindow = new System.Windows.Window
        {
            Title = $"布局预览 - {layout.Name}",
            Owner = this,
            Width = 920,
            Height = 600,
            MinWidth = 720,
            MinHeight = 480,
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
            Background = System.Windows.Media.Brushes.Black,
            Content = new System.Windows.Controls.Grid
            {
                Margin = new System.Windows.Thickness(20),
                Children =
                {
                    new System.Windows.Controls.Image
                    {
                        Source = previewImage,
                        Stretch = System.Windows.Media.Stretch.Uniform
                    }
                }
            }
        };

        previewWindow.ShowDialog();
    }

    private void OpenCreateModeEditor()
    {
        var editor = new ModeEditorWindow("新建自定义模式", _viewModel.ModeLayoutOptions)
        {
            Owner = this
        };

        if (editor.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var createdMode = _modeService.CreateCustomMode(
                editor.ModeName,
                editor.Description,
                editor.DesktopIconsVisible,
                editor.TaskbarVisible,
                editor.LayoutId);

            LoadModesIntoView(createdMode.Id);
            _viewModel.SetStatus($"已创建自定义模式“{createdMode.Name}”。");
        }
        catch (Exception ex)
        {
            _viewModel.SetStatus($"创建自定义模式失败：{ex.Message}");
        }
    }

    private void HandleDeletedModeReferences(string deletedModeId)
    {
        if (string.Equals(_appSettings.DefaultModeId, deletedModeId, StringComparison.OrdinalIgnoreCase))
        {
            _appSettings.DefaultModeId = AppSettings.DefaultModeIdValue;
            _settingsStore.Save(_appSettings);
        }

        if (string.Equals(_currentModeId, deletedModeId, StringComparison.OrdinalIgnoreCase))
        {
            _currentModeId = null;
        }
    }
}
