using System.Globalization;
using System.IO;
using WorkspaceManager.Application.Layouts;
using WorkspaceManager.Application.Modes;
using WorkspaceManager.Application.Wallpapers;
using WorkspaceManager.Domain.Layouts;
using WorkspaceManager.Domain.Modes;
using WorkspaceManager.Infrastructure.Configuration;
using WorkspaceManager.Interop.Desktop;
using WorkspaceManager.Interop.Hotkeys;
using WorkspaceManager.Interop.Startup;
using WorkspaceManager.UI.Services;
using WorkspaceManager.UI.ViewModels;

namespace WorkspaceManager.App;

public partial class MainWindow : System.Windows.Window
{
    private readonly AppSettings _appSettings;
    private readonly DesktopLayoutService _desktopLayoutService;
    private readonly ModeService _modeService;
    private readonly WallpaperRotationService _wallpaperRotationService;
    private readonly WallpaperAutoRotationService _wallpaperAutoRotationService;
    private readonly AppSettingsStore _settingsStore;
    private readonly DesktopIconService _desktopIconService;
    private readonly TaskbarService _taskbarService;
    private readonly StartupRegistrationService _startupRegistrationService;
    private readonly MainWindowViewDataBuilder _viewDataBuilder;
    private readonly MainWindowViewModel _viewModel;
    private GlobalHotkeyService? _desktopToggleHotkeyService;
    private GlobalHotkeyService? _showMainWindowHotkeyService;
    private string? _currentModeId;
    private bool _allowClose;
    private bool _isWallpaperChangeInProgress;

    public MainWindow(
        DesktopIconService desktopIconService,
        TaskbarService taskbarService,
        DesktopLayoutService desktopLayoutService,
        ModeService modeService,
        WallpaperRotationService wallpaperRotationService,
        WallpaperAutoRotationService wallpaperAutoRotationService,
        AppSettings appSettings,
        AppSettingsStore settingsStore,
        StartupRegistrationService startupRegistrationService,
        MainWindowViewDataBuilder viewDataBuilder)
    {
        InitializeComponent();
        _appSettings = appSettings;
        _desktopLayoutService = desktopLayoutService;
        _modeService = modeService;
        _wallpaperRotationService = wallpaperRotationService;
        _wallpaperAutoRotationService = wallpaperAutoRotationService;
        _settingsStore = settingsStore;
        _desktopIconService = desktopIconService;
        _taskbarService = taskbarService;
        _startupRegistrationService = startupRegistrationService;
        _viewDataBuilder = viewDataBuilder;
        _viewModel = new MainWindowViewModel();
        DataContext = _viewModel;

        StateChanged += OnStateChanged;
        Closing += OnClosing;
        _wallpaperAutoRotationService.RotationRequested += OnWallpaperAutoRotationRequestedAsync;

        LoadSettingsIntoView();
        LoadLayoutsIntoView();
        LoadModesIntoView();
        PrimeWallpaperCache();
        ApplyWallpaperAutoRotationSchedule();
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

    public void ScheduleStartupWallpaperRefresh()
    {
        if (!_appSettings.WallpaperChangeOnStartup)
        {
            return;
        }

        _ = Dispatcher.InvokeAsync(async () => await ChangeWallpaperAsync(true));
    }

    public void PrimeWallpaperCache()
    {
        _wallpaperRotationService.WarmUp(BuildWallpaperSourcesFromSettings());
    }

    public void ApplyWallpaperAutoRotationSchedule()
    {
        if (!_appSettings.WallpaperAutoRotateEnabled)
        {
            _wallpaperAutoRotationService.Configure(false, TimeSpan.Zero);
            _viewModel.SetWallpaperScheduleText("定时轮换：未开启");
            return;
        }

        var sources = BuildWallpaperSourcesFromSettings();
        if (!HasEnabledWallpaperSource(sources))
        {
            _wallpaperAutoRotationService.Configure(false, TimeSpan.Zero);
            _viewModel.SetWallpaperScheduleText("定时轮换：未启动，请先启用至少一个图源");
            return;
        }

        var interval = TimeSpan.FromMinutes(_appSettings.WallpaperRotationIntervalMinutes);
        _wallpaperAutoRotationService.Configure(true, interval);
        _viewModel.SetWallpaperScheduleText($"定时轮换：每 {_appSettings.WallpaperRotationIntervalMinutes} 分钟自动切换");
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
        _viewModel.RefreshWallpaperSourceStates();
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

    private async void ChangeWallpaperNow_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        await ChangeWallpaperAsync(false);
    }

    private void EnableAllWallpaperSources_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        foreach (var source in _viewModel.WallpaperSources)
        {
            source.Enabled = true;
        }

        _viewModel.SetStatus("已启用全部壁纸源，记得保存设置。");
    }

    private void AddWallpaperSource_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            var source = BuildWallpaperSourceFromDraft();
            _viewModel.WallpaperSources.Add(source);
            _viewModel.ClearWallpaperSourceDraft();
            _viewModel.SetStatus(
                source.Kind == WallpaperSourceKind.LocalFile
                    ? $"已添加本地图片“{source.Name}”，记得保存设置。"
                    : source.Kind == WallpaperSourceKind.LocalFolder
                        ? $"已添加本地文件夹“{source.Name}”，记得保存设置。"
                    : $"已添加自定义壁纸源“{source.Name}”，记得保存设置。");
        }
        catch (Exception ex)
        {
            _viewModel.SetStatus($"添加壁纸源失败：{ex.Message}");
        }
    }

    private void SelectLocalWallpaperFile_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            var normalizedPath = SelectLocalWallpaperFile();
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                return;
            }

            _viewModel.NewWallpaperSourceUrl = normalizedPath;
            if (string.IsNullOrWhiteSpace(_viewModel.NewWallpaperSourceName))
            {
                _viewModel.NewWallpaperSourceName = Path.GetFileNameWithoutExtension(normalizedPath);
            }

            _viewModel.SetStatus("已选中本地图片，点击“添加到图源库”后即可参与壁纸切换。");
        }
        catch (Exception ex)
        {
            _viewModel.SetStatus($"选择本地图片失败：{ex.Message}");
        }
    }

    private void SelectLocalWallpaperFolder_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            var normalizedPath = SelectLocalWallpaperFolder();
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                return;
            }

            _viewModel.NewWallpaperSourceUrl = normalizedPath;
            if (string.IsNullOrWhiteSpace(_viewModel.NewWallpaperSourceName))
            {
                _viewModel.NewWallpaperSourceName = Path.GetFileName(normalizedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            }

            _viewModel.SetStatus("已选中文件夹，点击“添加到图源库”后即可参与壁纸轮换。");
        }
        catch (Exception ex)
        {
            _viewModel.SetStatus($"选择本地文件夹失败：{ex.Message}");
        }
    }

    private void ReselectLocalWallpaperFile_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is not System.Windows.FrameworkElement { DataContext: WallpaperSourceViewModel source })
        {
            return;
        }

        if (source.Kind is not WallpaperSourceKind.LocalFile and not WallpaperSourceKind.LocalFolder)
        {
            _viewModel.SetStatus("只有本地图源支持重新选择。");
            return;
        }

        try
        {
            var previousPath = source.RequestUrl;
            var selectedPath = source.Kind == WallpaperSourceKind.LocalFolder
                ? SelectLocalWallpaperFolder(previousPath)
                : SelectLocalWallpaperFile(previousPath);
            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                return;
            }

            source.RequestUrl = selectedPath;
            var previousBaseName = source.Kind == WallpaperSourceKind.LocalFolder
                ? Path.GetFileName(previousPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                : Path.GetFileNameWithoutExtension(previousPath);
            if (string.IsNullOrWhiteSpace(source.Name)
                || string.Equals(source.Name, previousBaseName, StringComparison.OrdinalIgnoreCase))
            {
                source.Name = source.Kind == WallpaperSourceKind.LocalFolder
                    ? Path.GetFileName(selectedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                    : Path.GetFileNameWithoutExtension(selectedPath);
            }

            source.RefreshComputedState();
            _viewModel.SetStatus(
                source.Kind == WallpaperSourceKind.LocalFolder
                    ? $"已更新本地文件夹“{source.Name}”，记得保存设置。"
                    : $"已更新本地图片“{source.Name}”，记得保存设置。");
        }
        catch (Exception ex)
        {
            _viewModel.SetStatus(
                source.Kind == WallpaperSourceKind.LocalFolder
                    ? $"重新选择本地文件夹失败：{ex.Message}"
                    : $"重新选择本地图片失败：{ex.Message}");
        }
    }

    private void DeleteWallpaperSource_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is not System.Windows.FrameworkElement { DataContext: WallpaperSourceViewModel source })
        {
            return;
        }

        if (source.IsBuiltIn)
        {
            _viewModel.SetStatus("内置图源不支持删除，可直接取消启用。");
            return;
        }

        var result = System.Windows.MessageBox.Show(
            this,
            $"确定删除自定义壁纸源“{source.Name}”吗？",
            "删除壁纸源",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        _viewModel.WallpaperSources.Remove(source);
        _viewModel.SetStatus($"已删除自定义壁纸源“{source.Name}”，记得保存设置。");
    }

    private WallpaperSourceViewModel BuildWallpaperSourceFromDraft()
    {
        var sourceName = _viewModel.NewWallpaperSourceName.Trim();
        var location = _viewModel.NewWallpaperSourceUrl.Trim();
        if (string.IsNullOrWhiteSpace(location))
        {
            throw new InvalidOperationException("请输入图源地址，或选择本地图片/文件夹。");
        }

        if (WallpaperSourceSetting.TryNormalizeRemoteUrl(location, out var normalizedRemoteUrl))
        {
            if (string.IsNullOrWhiteSpace(sourceName))
            {
                throw new InvalidOperationException("请输入图源名称。");
            }

            EnsureUniqueWallpaperSource(WallpaperSourceKind.RemoteUrl, normalizedRemoteUrl, "这个地址已经在图源列表里了。");
            return new WallpaperSourceViewModel
            {
                Id = $"custom-{Guid.NewGuid():N}",
                Name = sourceName,
                RequestUrl = normalizedRemoteUrl,
                Kind = WallpaperSourceKind.RemoteUrl,
                Enabled = true,
                IsBuiltIn = false
            };
        }

        if (WallpaperSourceSetting.TryNormalizeLocalFilePath(location, out var normalizedLocalPath))
        {
            if (!File.Exists(normalizedLocalPath))
            {
                throw new InvalidOperationException("选择的本地图片不存在。");
            }

            var resolvedName = string.IsNullOrWhiteSpace(sourceName)
                ? Path.GetFileNameWithoutExtension(normalizedLocalPath)
                : sourceName;
            EnsureUniqueWallpaperSource(WallpaperSourceKind.LocalFile, normalizedLocalPath, "这张本地图片已经在图源列表里了。");
            return new WallpaperSourceViewModel
            {
                Id = $"local-{Guid.NewGuid():N}",
                Name = resolvedName,
                RequestUrl = normalizedLocalPath,
                Kind = WallpaperSourceKind.LocalFile,
                Enabled = true,
                IsBuiltIn = false
            };
        }

        if (WallpaperSourceSetting.TryNormalizeLocalDirectoryPath(location, out var normalizedLocalDirectory))
        {
            if (!Directory.Exists(normalizedLocalDirectory))
            {
                throw new InvalidOperationException("选择的本地文件夹不存在。");
            }

            var resolvedName = string.IsNullOrWhiteSpace(sourceName)
                ? Path.GetFileName(normalizedLocalDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                : sourceName;
            EnsureUniqueWallpaperSource(WallpaperSourceKind.LocalFolder, normalizedLocalDirectory, "这个本地文件夹已经在图源列表里了。");
            return new WallpaperSourceViewModel
            {
                Id = $"folder-{Guid.NewGuid():N}",
                Name = resolvedName,
                RequestUrl = normalizedLocalDirectory,
                Kind = WallpaperSourceKind.LocalFolder,
                Enabled = true,
                IsBuiltIn = false
            };
        }

        throw new InvalidOperationException("请输入有效的 http/https 地址，或选择 JPG、PNG、BMP、GIF、WEBP 图片，或一个本地文件夹。");
    }

    private void EnsureUniqueWallpaperSource(WallpaperSourceKind kind, string normalizedLocation, string errorMessage)
    {
        var exists = _viewModel.WallpaperSources.Any(source =>
            source.Kind == kind
            && WallpaperSourceSetting.TryNormalizeLocation(source.RequestUrl, source.Kind, out var existingLocation)
            && string.Equals(existingLocation, normalizedLocation, StringComparison.OrdinalIgnoreCase));

        if (exists)
        {
            throw new InvalidOperationException(errorMessage);
        }
    }

    private string? SelectLocalWallpaperFile(string? currentPath = null)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择本地壁纸图片",
            Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp",
            CheckFileExists = true,
            CheckPathExists = true,
            Multiselect = false
        };

        if (WallpaperSourceSetting.TryNormalizeLocalFilePath(currentPath, out var normalizedCurrentPath))
        {
            var initialDirectory = Path.GetDirectoryName(normalizedCurrentPath);
            if (!string.IsNullOrWhiteSpace(initialDirectory) && Directory.Exists(initialDirectory))
            {
                dialog.InitialDirectory = initialDirectory;
                dialog.FileName = Path.GetFileName(normalizedCurrentPath);
            }
        }

        if (dialog.ShowDialog(this) != true)
        {
            return null;
        }

        if (!WallpaperSourceSetting.TryNormalizeLocalFilePath(dialog.FileName, out var normalizedPath))
        {
            throw new InvalidOperationException("当前选择的文件不是受支持的本地图片。");
        }

        return normalizedPath;
    }

    private string? SelectLocalWallpaperFolder(string? currentPath = null)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "选择包含壁纸图片的文件夹",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        if (WallpaperSourceSetting.TryNormalizeLocalDirectoryPath(currentPath, out var normalizedCurrentPath)
            && Directory.Exists(normalizedCurrentPath))
        {
            dialog.InitialDirectory = normalizedCurrentPath;
        }

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return null;
        }

        if (!WallpaperSourceSetting.TryNormalizeLocalDirectoryPath(dialog.SelectedPath, out var normalizedPath))
        {
            throw new InvalidOperationException("当前选择的文件夹路径无效。");
        }

        return normalizedPath;
    }

    private async Task ChangeWallpaperAsync(bool isStartup, bool isScheduled = false)
    {
        if (_isWallpaperChangeInProgress)
        {
            if (!isStartup && !isScheduled)
            {
                _viewModel.SetStatus("壁纸切换进行中。");
            }

            return;
        }

        _isWallpaperChangeInProgress = true;
        _viewModel.SetWallpaperChangeInProgress(true);
        _viewModel.SetStatus(isStartup
            ? "正在为启动流程切换桌面壁纸..."
            : isScheduled
                ? "已到轮换时间，正在切换桌面壁纸..."
                : "正在切换桌面壁纸...");
        try
        {
            var wallpaperSources = isStartup || isScheduled
                ? BuildWallpaperSourcesFromSettings()
                : BuildWallpaperSourcesFromView();
            if (!HasEnabledWallpaperSource(wallpaperSources))
            {
                throw new InvalidOperationException("请先启用至少一个壁纸源。");
            }

            var result = await _wallpaperRotationService.ApplyRandomAsync(wallpaperSources);
            _viewModel.SetStatus(isStartup
                ? $"启动时已使用“{result.SourceName}”切换桌面壁纸。"
                : isScheduled
                    ? $"已按定时轮换使用“{result.SourceName}”切换桌面壁纸。"
                    : $"已使用“{result.SourceName}”切换桌面壁纸。");
        }
        catch (Exception ex)
        {
            _viewModel.SetStatus(isStartup
                ? $"启动时切换壁纸失败：{ex.Message}"
                : isScheduled
                    ? $"定时轮换壁纸失败：{ex.Message}"
                    : $"切换桌面壁纸失败：{ex.Message}");
        }
        finally
        {
            _isWallpaperChangeInProgress = false;
            _viewModel.SetWallpaperChangeInProgress(false);
            ApplyWallpaperAutoRotationSchedule();
        }
    }

    private List<WallpaperSourceSetting> BuildWallpaperSourcesFromView()
    {
        return _viewModel.WallpaperSources
            .Select(source => new WallpaperSourceSetting
            {
                Id = source.Id,
                Name = source.Name.Trim(),
                RequestUrl = source.RequestUrl.Trim(),
                Kind = source.Kind,
                Enabled = source.Enabled
            })
            .ToList();
    }

    private List<WallpaperSourceSetting> BuildWallpaperSourcesFromSettings()
    {
        return _appSettings.WallpaperSources
            .Select(source => new WallpaperSourceSetting
            {
                Id = source.Id,
                Name = source.Name,
                RequestUrl = source.RequestUrl,
                Kind = source.Kind,
                Enabled = source.Enabled
            })
            .ToList();
    }

    private async Task OnWallpaperAutoRotationRequestedAsync()
    {
        var operation = Dispatcher.InvokeAsync(() => ChangeWallpaperAsync(false, true));
        await operation.Task.Unwrap();
    }

    private static int ParseWallpaperRotationIntervalMinutes(string? value)
    {
        if (!TryParseWallpaperRotationIntervalMinutes(value, out var minutes))
        {
            throw new InvalidOperationException("轮换间隔必须是 1 到 1440 之间的整数分钟。");
        }

        return minutes;
    }

    private static bool TryParseWallpaperRotationIntervalMinutes(string? value, out int minutes)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out minutes))
        {
            return false;
        }

        return minutes is >= 1 and <= 1440;
    }

    private static int ResolveWallpaperRotationIntervalMinutesForSave(string? value, int fallback, bool autoRotateEnabled)
    {
        if (autoRotateEnabled)
        {
            return ParseWallpaperRotationIntervalMinutes(value);
        }

        return TryParseWallpaperRotationIntervalMinutes(value, out var minutes)
            ? minutes
            : NormalizeWallpaperRotationIntervalMinutes(fallback);
    }

    private static int NormalizeWallpaperRotationIntervalMinutes(int minutes)
    {
        return minutes is >= 1 and <= 1440
            ? minutes
            : AppSettings.DefaultWallpaperRotationIntervalMinutes;
    }

    private static bool HasEnabledWallpaperSource(IReadOnlyCollection<WallpaperSourceSetting> sources)
    {
        return sources.Any(source =>
        {
            if (!source.Enabled)
            {
                return false;
            }

            return source.Kind switch
            {
                WallpaperSourceKind.LocalFile => WallpaperSourceSetting.TryNormalizeLocalFilePath(source.RequestUrl, out var localPath)
                    && File.Exists(localPath),
                WallpaperSourceKind.LocalFolder => WallpaperSourceSetting.TryNormalizeLocalDirectoryPath(source.RequestUrl, out var localDirectory)
                    && Directory.Exists(localDirectory)
                    && WallpaperSourceSetting.GetSupportedLocalImageFiles(localDirectory).Count > 0,
                _ => WallpaperSourceSetting.TryNormalizeRemoteUrl(source.RequestUrl, out _)
            };
        });
    }

    private void SaveWallpaperSettings_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            var wallpaperSources = BuildWallpaperSourcesFromView();
            var rotationMinutes = ResolveWallpaperRotationIntervalMinutesForSave(
                _viewModel.WallpaperRotationIntervalMinutesInput,
                _appSettings.WallpaperRotationIntervalMinutes,
                _viewModel.WallpaperAutoRotateEnabled);
            if ((_viewModel.WallpaperChangeOnStartupEnabled || _viewModel.WallpaperAutoRotateEnabled) && !HasEnabledWallpaperSource(wallpaperSources))
            {
                throw new InvalidOperationException("启用壁纸自动功能时，至少需要一个可用图源。");
            }

            _appSettings.WallpaperChangeOnStartup = _viewModel.WallpaperChangeOnStartupEnabled;
            _appSettings.WallpaperAutoRotateEnabled = _viewModel.WallpaperAutoRotateEnabled;
            _appSettings.WallpaperRotationIntervalMinutes = rotationMinutes;
            _appSettings.WallpaperSources = wallpaperSources;
            _settingsStore.Save(_appSettings);
            _viewModel.SetWallpaperRotationIntervalMinutesInput(_appSettings.WallpaperRotationIntervalMinutes.ToString(CultureInfo.InvariantCulture));
            PrimeWallpaperCache();
            ApplyWallpaperAutoRotationSchedule();
            _viewModel.SetStatus(_appSettings.WallpaperAutoRotateEnabled
                ? $"壁纸设置已保存，已开启每 {_appSettings.WallpaperRotationIntervalMinutes} 分钟轮换。"
                : "壁纸设置已保存。下次切换会优先使用预取缓存。");
        }
        catch (Exception ex)
        {
            _viewModel.SetStatus($"保存壁纸设置失败：{ex.Message}");
        }
    }

    private void ReloadWallpaperSettings_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var latest = _settingsStore.Load();
        _appSettings.WallpaperChangeOnStartup = latest.WallpaperChangeOnStartup;
        _appSettings.WallpaperAutoRotateEnabled = latest.WallpaperAutoRotateEnabled;
        _appSettings.WallpaperRotationIntervalMinutes = latest.WallpaperRotationIntervalMinutes;
        _appSettings.WallpaperSources = latest.WallpaperSources
            .Select(source => new WallpaperSourceSetting
            {
                Id = source.Id,
                Name = source.Name,
                RequestUrl = source.RequestUrl,
                Kind = source.Kind,
                Enabled = source.Enabled
            })
            .ToList();

        LoadSettingsIntoView();
        PrimeWallpaperCache();
        ApplyWallpaperAutoRotationSchedule();
        _viewModel.SetStatus("壁纸设置已重新加载。");
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
            _viewModel.SetDefaultModeName(_viewDataBuilder.FindModeName(_modeService.GetModes(), _appSettings.DefaultModeId));
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
        _appSettings.WallpaperChangeOnStartup = latest.WallpaperChangeOnStartup;
        _appSettings.WallpaperAutoRotateEnabled = latest.WallpaperAutoRotateEnabled;
        _appSettings.WallpaperRotationIntervalMinutes = latest.WallpaperRotationIntervalMinutes;
        _appSettings.WallpaperSources = latest.WallpaperSources
            .Select(source => new WallpaperSourceSetting
            {
                Id = source.Id,
                Name = source.Name,
                RequestUrl = source.RequestUrl,
                Kind = source.Kind,
                Enabled = source.Enabled
            })
            .ToList();
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
        PrimeWallpaperCache();
        ApplyWallpaperAutoRotationSchedule();
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

        _wallpaperAutoRotationService.RotationRequested -= OnWallpaperAutoRotationRequestedAsync;
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
        var modes = _modeService.GetModes();
        var defaultModeName = _viewDataBuilder.FindModeName(
            modes,
            string.IsNullOrWhiteSpace(_appSettings.DefaultModeId)
                ? AppSettings.DefaultModeIdValue
                : _appSettings.DefaultModeId);

        _viewDataBuilder.ApplySettings(_viewModel, _appSettings, defaultModeName);
        _viewModel.ClearWallpaperSourceDraft();
    }

    private void LoadLayoutsIntoView()
    {
        var layouts = _desktopLayoutService.GetSavedLayouts();
        _viewModel.SetLayouts(_viewDataBuilder.BuildLayouts(layouts, _desktopLayoutService.GetPreviewPath));
        _viewModel.SetModeLayoutOptions(_viewDataBuilder.BuildModeLayoutOptions(layouts));
        if (_viewModel.SelectedMode is not null)
        {
            _viewModel.SetSelectedModeLayoutId(_viewModel.SelectedMode.LayoutId);
        }
        LoadModesIntoView(_viewModel.SelectedMode?.Id, layouts);
    }

    private void LoadModesIntoView(string? selectedModeId = null, IReadOnlyList<DesktopLayoutSnapshot>? layouts = null)
    {
        var savedLayouts = layouts ?? _desktopLayoutService.GetSavedLayouts();
        var modes = _modeService.GetModes();
        _viewModel.SetModes(_viewDataBuilder.BuildModes(modes, savedLayouts, _appSettings.DefaultModeId, _currentModeId));
        if (!string.IsNullOrWhiteSpace(selectedModeId))
        {
            _viewModel.SelectedMode = _viewModel.Modes.FirstOrDefault(mode => string.Equals(mode.Id, selectedModeId, StringComparison.OrdinalIgnoreCase))
                ?? _viewModel.SelectedMode;
        }

        _viewModel.SetModeLayoutOptions(_viewDataBuilder.BuildModeLayoutOptions(savedLayouts));
        _viewModel.SetModeOptions(_viewDataBuilder.BuildModeOptions(modes));
        _viewModel.SetSelectedModeLayoutId(_viewModel.SelectedMode?.LayoutId);
        _viewModel.SetCurrentMode(_viewDataBuilder.FindModeName(modes, _currentModeId));
        _viewModel.SetDefaultModeName(_viewDataBuilder.FindModeName(modes, _appSettings.DefaultModeId));
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

        var previewImage = _viewDataBuilder.LoadPreviewImage(layout.PreviewImagePath);
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
