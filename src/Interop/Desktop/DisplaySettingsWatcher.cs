using Microsoft.Win32;

namespace WorkspaceManager.Interop.Desktop;

public sealed class DisplaySettingsWatcher : IDisposable
{
    private bool _isStarted;
    private bool _isDisposed;

    public event EventHandler? DisplaySettingsChanging;

    public event EventHandler? DisplaySettingsChanged;

    public void Start()
    {
        ThrowIfDisposed();
        if (_isStarted)
        {
            return;
        }

        SystemEvents.DisplaySettingsChanging += OnDisplaySettingsChanging;
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        _isStarted = true;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        if (_isStarted)
        {
            SystemEvents.DisplaySettingsChanging -= OnDisplaySettingsChanging;
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            _isStarted = false;
        }

        _isDisposed = true;
    }

    private void OnDisplaySettingsChanging(object? sender, EventArgs e)
    {
        DisplaySettingsChanging?.Invoke(this, EventArgs.Empty);
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        DisplaySettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }
}
