using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace WorkspaceManager.App.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private string _layoutNameInput = string.Empty;
    private bool _launchAtStartupEnabled;
    private LayoutSummaryViewModel? _selectedLayout;
    private bool _isDesktopIconsVisible;
    private bool _isTaskbarVisible;
    private bool _closeToTrayOnCloseEnabled;
    private bool _minimizeToTrayOnMinimizeEnabled;
    private bool _startMinimizedToTrayEnabled;
    private string _desktopToggleHotkeyInput = AppSettings.DefaultDesktopToggleHotkey;
    private string _showMainWindowHotkeyInput = AppSettings.DefaultShowMainWindowHotkey;
    private string _statusMessage = "桌面图标、托盘和快捷键原型已可用。";
    private string _desktopIconStateText = "桌面图标状态：未读取";
    private string _taskbarStateText = "任务栏状态：未读取";
    private string _hotkeyText = "全局快捷键：未启用";

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

    public event PropertyChangedEventHandler? PropertyChanged;

    public void SetDesktopIconState(bool isVisible)
    {
        IsDesktopIconsVisible = isVisible;
        DesktopIconStateText = $"桌面图标状态：{(isVisible ? "显示中" : "已隐藏")}";
        StatusMessage = "托盘与桌面图标切换原型已可用。";
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
        HotkeyText = $"图标 {desktopToggleHotkey} · 主窗口 {showMainWindowHotkey}";
    }

    public void SetLaunchAtStartup(bool enabled)
    {
        LaunchAtStartupEnabled = enabled;
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
