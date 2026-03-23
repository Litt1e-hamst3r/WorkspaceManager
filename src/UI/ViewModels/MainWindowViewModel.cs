using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using WorkspaceManager.Infrastructure.Configuration;

namespace WorkspaceManager.UI.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private string _layoutNameInput = string.Empty;
    private string _newWallpaperSourceName = string.Empty;
    private string _newWallpaperSourceUrl = string.Empty;
    private bool _launchAtStartupEnabled;
    private string _defaultModeId = AppSettings.DefaultModeIdValue;
    private LayoutSummaryViewModel? _selectedLayout;
    private DesktopModeViewModel? _selectedMode;
    private string _selectedModeLayoutId = string.Empty;
    private bool _isDesktopIconsVisible;
    private bool _isTaskbarVisible;
    private bool _closeToTrayOnCloseEnabled;
    private bool _minimizeToTrayOnMinimizeEnabled;
    private bool _startMinimizedToTrayEnabled;
    private bool _wallpaperChangeOnStartupEnabled;
    private bool _wallpaperAutoRotateEnabled;
    private bool _isWallpaperChangeInProgress;
    private string _wallpaperRotationIntervalMinutesInput = AppSettings.DefaultWallpaperRotationIntervalMinutes.ToString();
    private string _wallpaperFavoriteSaveDirectoryInput = AppSettings.GetDefaultWallpaperFavoriteSaveDirectory();
    private string _wallpaperScheduleText = "定时轮换：未开启";
    private string _wallpaperSourceSummaryText = "已启用 0/0 · 自定义 0";
    private string _desktopToggleHotkeyInput = AppSettings.DefaultDesktopToggleHotkey;
    private string _showMainWindowHotkeyInput = AppSettings.DefaultShowMainWindowHotkey;
    private string _statusMessage = "桌面图标、托盘和快捷键原型已可用。";
    private string _desktopIconStateText = "桌面图标状态：未读取";
    private string _taskbarStateText = "任务栏状态：未读取";
    private string _hotkeyText = "全局快捷键：未启用";
    private string _currentModeText = "当前模式：未应用";
    private string _defaultModeText = "默认模式：默认模式";

    public MainWindowViewModel()
    {
        WallpaperSources.CollectionChanged += OnWallpaperSourcesCollectionChanged;
        RefreshWallpaperSourceSummary();
    }

    public string AppName => "Workspace Manager";

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage == value)
            {
                return;
            }

            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public string DesktopIconStateText
    {
        get => _desktopIconStateText;
        private set
        {
            if (_desktopIconStateText == value)
            {
                return;
            }

            _desktopIconStateText = value;
            OnPropertyChanged();
        }
    }

    public bool IsDesktopIconsVisible
    {
        get => _isDesktopIconsVisible;
        private set
        {
            if (_isDesktopIconsVisible == value)
            {
                return;
            }

            _isDesktopIconsVisible = value;
            OnPropertyChanged();
        }
    }

    public string LayoutNameInput
    {
        get => _layoutNameInput;
        set
        {
            if (_layoutNameInput == value)
            {
                return;
            }

            _layoutNameInput = value;
            OnPropertyChanged();
        }
    }

    public string NewWallpaperSourceName
    {
        get => _newWallpaperSourceName;
        set
        {
            if (_newWallpaperSourceName == value)
            {
                return;
            }

            _newWallpaperSourceName = value;
            OnPropertyChanged();
        }
    }

    public string NewWallpaperSourceUrl
    {
        get => _newWallpaperSourceUrl;
        set
        {
            if (_newWallpaperSourceUrl == value)
            {
                return;
            }

            _newWallpaperSourceUrl = value;
            OnPropertyChanged();
        }
    }

    public string DefaultModeId
    {
        get => _defaultModeId;
        set
        {
            if (_defaultModeId == value)
            {
                return;
            }

            _defaultModeId = value;
            OnPropertyChanged();
        }
    }

    public string TaskbarStateText
    {
        get => _taskbarStateText;
        private set
        {
            if (_taskbarStateText == value)
            {
                return;
            }

            _taskbarStateText = value;
            OnPropertyChanged();
        }
    }

    public bool IsTaskbarVisible
    {
        get => _isTaskbarVisible;
        private set
        {
            if (_isTaskbarVisible == value)
            {
                return;
            }

            _isTaskbarVisible = value;
            OnPropertyChanged();
        }
    }

    public bool LaunchAtStartupEnabled
    {
        get => _launchAtStartupEnabled;
        set
        {
            if (_launchAtStartupEnabled == value)
            {
                return;
            }

            _launchAtStartupEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool MinimizeToTrayOnMinimizeEnabled
    {
        get => _minimizeToTrayOnMinimizeEnabled;
        set
        {
            if (_minimizeToTrayOnMinimizeEnabled == value)
            {
                return;
            }

            _minimizeToTrayOnMinimizeEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool CloseToTrayOnCloseEnabled
    {
        get => _closeToTrayOnCloseEnabled;
        set
        {
            if (_closeToTrayOnCloseEnabled == value)
            {
                return;
            }

            _closeToTrayOnCloseEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool StartMinimizedToTrayEnabled
    {
        get => _startMinimizedToTrayEnabled;
        set
        {
            if (_startMinimizedToTrayEnabled == value)
            {
                return;
            }

            _startMinimizedToTrayEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool WallpaperChangeOnStartupEnabled
    {
        get => _wallpaperChangeOnStartupEnabled;
        set
        {
            if (_wallpaperChangeOnStartupEnabled == value)
            {
                return;
            }

            _wallpaperChangeOnStartupEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool WallpaperAutoRotateEnabled
    {
        get => _wallpaperAutoRotateEnabled;
        set
        {
            if (_wallpaperAutoRotateEnabled == value)
            {
                return;
            }

            _wallpaperAutoRotateEnabled = value;
            OnPropertyChanged();
        }
    }

    public string WallpaperRotationIntervalMinutesInput
    {
        get => _wallpaperRotationIntervalMinutesInput;
        set
        {
            if (_wallpaperRotationIntervalMinutesInput == value)
            {
                return;
            }

            _wallpaperRotationIntervalMinutesInput = value;
            OnPropertyChanged();
        }
    }

    public string WallpaperScheduleText
    {
        get => _wallpaperScheduleText;
        private set
        {
            if (_wallpaperScheduleText == value)
            {
                return;
            }

            _wallpaperScheduleText = value;
            OnPropertyChanged();
        }
    }

    public string WallpaperSourceSummaryText
    {
        get => _wallpaperSourceSummaryText;
        private set
        {
            if (_wallpaperSourceSummaryText == value)
            {
                return;
            }

            _wallpaperSourceSummaryText = value;
            OnPropertyChanged();
        }
    }

    public string WallpaperFavoriteSaveDirectoryInput
    {
        get => _wallpaperFavoriteSaveDirectoryInput;
        set
        {
            if (_wallpaperFavoriteSaveDirectoryInput == value)
            {
                return;
            }

            _wallpaperFavoriteSaveDirectoryInput = value;
            OnPropertyChanged();
        }
    }

    public bool IsWallpaperChangeInProgress
    {
        get => _isWallpaperChangeInProgress;
        private set
        {
            if (_isWallpaperChangeInProgress == value)
            {
                return;
            }

            _isWallpaperChangeInProgress = value;
            OnPropertyChanged();
        }
    }

    public string DesktopToggleHotkeyInput
    {
        get => _desktopToggleHotkeyInput;
        set
        {
            if (_desktopToggleHotkeyInput == value)
            {
                return;
            }

            _desktopToggleHotkeyInput = value;
            OnPropertyChanged();
        }
    }

    public string ShowMainWindowHotkeyInput
    {
        get => _showMainWindowHotkeyInput;
        set
        {
            if (_showMainWindowHotkeyInput == value)
            {
                return;
            }

            _showMainWindowHotkeyInput = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<LayoutSummaryViewModel> SavedLayouts { get; } = [];

    public ObservableCollection<DesktopModeViewModel> Modes { get; } = [];

    public ObservableCollection<ModeLayoutOptionViewModel> ModeLayoutOptions { get; } = [];

    public ObservableCollection<ModeOptionViewModel> ModeOptions { get; } = [];

    public ObservableCollection<WallpaperSourceViewModel> WallpaperSources { get; } = [];

    public LayoutSummaryViewModel? SelectedLayout
    {
        get => _selectedLayout;
        set
        {
            if (_selectedLayout == value)
            {
                return;
            }

            _selectedLayout = value;
            OnPropertyChanged();
        }
    }

    public DesktopModeViewModel? SelectedMode
    {
        get => _selectedMode;
        set
        {
            if (_selectedMode == value)
            {
                return;
            }

            _selectedMode = value;
            OnPropertyChanged();
            SelectedModeLayoutId = _selectedMode?.LayoutId ?? string.Empty;
        }
    }

    public string SelectedModeLayoutId
    {
        get => _selectedModeLayoutId;
        set
        {
            if (_selectedModeLayoutId == value)
            {
                return;
            }

            _selectedModeLayoutId = value;
            OnPropertyChanged();
        }
    }

    public string HotkeyText
    {
        get => _hotkeyText;
        private set
        {
            if (_hotkeyText == value)
            {
                return;
            }

            _hotkeyText = value;
            OnPropertyChanged();
        }
    }

    public string CurrentModeText
    {
        get => _currentModeText;
        private set
        {
            if (_currentModeText == value)
            {
                return;
            }

            _currentModeText = value;
            OnPropertyChanged();
        }
    }

    public string DefaultModeText
    {
        get => _defaultModeText;
        private set
        {
            if (_defaultModeText == value)
            {
                return;
            }

            _defaultModeText = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void SetDesktopIconState(bool isVisible)
    {
        IsDesktopIconsVisible = isVisible;
        DesktopIconStateText = $"桌面图标状态：{(isVisible ? "显示中" : "已隐藏")}";
    }

    public void SetStatus(string message)
    {
        StatusMessage = message;
    }

    public void SetTaskbarState(bool isVisible)
    {
        IsTaskbarVisible = isVisible;
        TaskbarStateText = $"任务栏状态：{(isVisible ? "显示中" : "已隐藏")}";
    }

    public void SetHotkeys(string desktopToggleHotkey, string showMainWindowHotkey)
    {
        HotkeyText = $"图标 {desktopToggleHotkey} · 主窗口显隐 {showMainWindowHotkey}";
    }

    public void SetLaunchAtStartup(bool enabled)
    {
        LaunchAtStartupEnabled = enabled;
    }

    public void SetDefaultModeId(string modeId)
    {
        DefaultModeId = string.IsNullOrWhiteSpace(modeId)
            ? AppSettings.DefaultModeIdValue
            : modeId;
    }

    public void SetStartMinimizedToTray(bool enabled)
    {
        StartMinimizedToTrayEnabled = enabled;
    }

    public void SetMinimizeToTrayOnMinimize(bool enabled)
    {
        MinimizeToTrayOnMinimizeEnabled = enabled;
    }

    public void SetCloseToTrayOnClose(bool enabled)
    {
        CloseToTrayOnCloseEnabled = enabled;
    }

    public void SetWallpaperChangeOnStartup(bool enabled)
    {
        WallpaperChangeOnStartupEnabled = enabled;
    }

    public void SetWallpaperAutoRotateEnabled(bool enabled)
    {
        WallpaperAutoRotateEnabled = enabled;
    }

    public void SetWallpaperRotationIntervalMinutesInput(string value)
    {
        WallpaperRotationIntervalMinutesInput = value;
    }

    public void SetWallpaperFavoriteSaveDirectoryInput(string value)
    {
        WallpaperFavoriteSaveDirectoryInput = value;
    }

    public void SetWallpaperScheduleText(string value)
    {
        WallpaperScheduleText = value;
    }

    public void SetWallpaperChangeInProgress(bool inProgress)
    {
        IsWallpaperChangeInProgress = inProgress;
    }

    public void SetLayoutNameInput(string value)
    {
        LayoutNameInput = value;
    }

    public void SetDesktopToggleHotkeyInput(string value)
    {
        DesktopToggleHotkeyInput = value;
    }

    public void SetShowMainWindowHotkeyInput(string value)
    {
        ShowMainWindowHotkeyInput = value;
    }

    public void SetLayouts(IEnumerable<LayoutSummaryViewModel> layouts)
    {
        SavedLayouts.Clear();
        foreach (var layout in layouts)
        {
            SavedLayouts.Add(layout);
        }

        SelectedLayout = SavedLayouts.FirstOrDefault();
    }

    public void SetModes(IEnumerable<DesktopModeViewModel> modes)
    {
        var selectedModeId = SelectedMode?.Id;
        Modes.Clear();
        foreach (var mode in modes)
        {
            Modes.Add(mode);
        }

        SelectedMode = Modes.FirstOrDefault(mode => string.Equals(mode.Id, selectedModeId, StringComparison.OrdinalIgnoreCase))
            ?? Modes.FirstOrDefault();
    }

    public void SetModeLayoutOptions(IEnumerable<ModeLayoutOptionViewModel> options)
    {
        ModeLayoutOptions.Clear();
        foreach (var option in options)
        {
            ModeLayoutOptions.Add(option);
        }
    }

    public void SetModeOptions(IEnumerable<ModeOptionViewModel> options)
    {
        ModeOptions.Clear();
        foreach (var option in options)
        {
            ModeOptions.Add(option);
        }
    }

    public void SetWallpaperSources(IEnumerable<WallpaperSourceViewModel> sources)
    {
        foreach (var existingSource in WallpaperSources)
        {
            existingSource.PropertyChanged -= OnWallpaperSourcePropertyChanged;
        }

        WallpaperSources.Clear();
        foreach (var source in sources)
        {
            WallpaperSources.Add(source);
        }

        RefreshWallpaperSourceStates();
        RefreshWallpaperSourceSummary();
    }

    public void ClearWallpaperSourceDraft()
    {
        NewWallpaperSourceName = string.Empty;
        NewWallpaperSourceUrl = string.Empty;
    }

    public void RefreshWallpaperSourceStates()
    {
        foreach (var source in WallpaperSources)
        {
            source.RefreshComputedState();
        }
    }

    public void SetSelectedModeLayoutId(string? layoutId)
    {
        SelectedModeLayoutId = layoutId ?? string.Empty;
    }

    public void SetCurrentMode(string? modeName)
    {
        CurrentModeText = $"当前模式：{(string.IsNullOrWhiteSpace(modeName) ? "未应用" : modeName)}";
    }

    public void SetDefaultModeName(string? modeName)
    {
        DefaultModeText = $"默认模式：{(string.IsNullOrWhiteSpace(modeName) ? "未设置" : modeName)}";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void OnWallpaperSourcesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (WallpaperSourceViewModel source in e.OldItems)
            {
                source.PropertyChanged -= OnWallpaperSourcePropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (WallpaperSourceViewModel source in e.NewItems)
            {
                source.PropertyChanged -= OnWallpaperSourcePropertyChanged;
                source.PropertyChanged += OnWallpaperSourcePropertyChanged;
                source.RefreshComputedState();
            }
        }

        RefreshWallpaperSourceSummary();
    }

    private void OnWallpaperSourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(WallpaperSourceViewModel.Enabled), StringComparison.Ordinal))
        {
            RefreshWallpaperSourceSummary();
        }
    }

    private void RefreshWallpaperSourceSummary()
    {
        var totalCount = WallpaperSources.Count;
        var enabledCount = WallpaperSources.Count(source => source.Enabled);
        var customCount = WallpaperSources.Count(source => !source.IsBuiltIn);
        WallpaperSourceSummaryText = $"已启用 {enabledCount}/{totalCount} · 自定义 {customCount}";
    }
}
