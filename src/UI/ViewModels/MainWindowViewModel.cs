using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WorkspaceManager.UI.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private string _statusMessage = "M1 原型已接线，下一步可继续接入热键与模式系统。";
    private string _desktopIconStateText = "桌面图标状态：未读取";

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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
