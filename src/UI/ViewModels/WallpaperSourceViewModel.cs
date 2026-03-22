using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WorkspaceManager.UI.ViewModels;

public sealed class WallpaperSourceViewModel : INotifyPropertyChanged
{
    private bool _enabled = true;

    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string RequestUrl { get; set; } = string.Empty;

    public bool IsBuiltIn { get; set; }

    public bool CanDelete => !IsBuiltIn;

    public string SourceTypeText => IsBuiltIn ? "内置源" : "自定义";

    public string EnabledText => Enabled ? "已启用" : "已停用";

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value)
            {
                return;
            }

            _enabled = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Enabled)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EnabledText)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
