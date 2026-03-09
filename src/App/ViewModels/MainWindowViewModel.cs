using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace WorkspaceManager.App.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private string _layoutNameInput = string.Empty;
    private bool _launchAtStartupEnabled;
    private LayoutSummaryViewModel? _selectedLayout;
    private bool _startMinimizedToTrayEnabled;
    private string _statusMessage = "桌面图标、托盘和快捷键原型已可用。";
    private string _desktopIconStateText = "桌面图标状态：未读取";
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
        DesktopIconStateText = $"桌面图标状态：{(isVisible ? "显示中" : "已隐藏")}";
        StatusMessage = "托盘与桌面图标切换原型已可用。";
    }

    public void SetStatus(string message)
    {
        StatusMessage = message;
    }

    public void SetHotkey(string hotkey)
    {
        HotkeyText = $"全局快捷键：{hotkey}";
    }

    public void SetLaunchAtStartup(bool enabled)
    {
        LaunchAtStartupEnabled = enabled;
    }

    public void SetStartMinimizedToTray(bool enabled)
    {
        StartMinimizedToTrayEnabled = enabled;
    }

    public void SetLayoutNameInput(string value)
    {
        LayoutNameInput = value;
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
