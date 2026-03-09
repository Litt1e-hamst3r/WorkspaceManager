using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WorkspaceManager.App.ViewModels;

namespace WorkspaceManager.App;

public partial class MainWindow : System.Windows.Window
{
    private readonly AppSettings _appSettings;
    private readonly DesktopLayoutService _desktopLayoutService;
    private readonly AppSettingsStore _settingsStore;
    private readonly DesktopIconService _desktopIconService;
    private readonly TaskbarService _taskbarService;
    private readonly StartupRegistrationService _startupRegistrationService;
    private readonly MainWindowViewModel _viewModel;
    private GlobalHotkeyService? _globalHotkeyService;
    private bool _allowClose;

    public MainWindow(
        DesktopIconService desktopIconService,
        TaskbarService taskbarService,
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
        _taskbarService = taskbarService;
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

    private void ToggleHeaderDetails_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        _viewModel.ToggleHeaderExpanded();
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
}
