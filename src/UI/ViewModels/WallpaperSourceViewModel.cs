using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WorkspaceManager.UI.ViewModels;

public sealed class WallpaperSourceViewModel : INotifyPropertyChanged
{
    private bool _enabled = true;

    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string RequestUrl { get; set; } = string.Empty;

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
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
