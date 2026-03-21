using System.Windows;
using System.Windows.Threading;
using WorkspaceManager.Domain.Layouts;
using WorkspaceManager.Interop.Desktop;

namespace WorkspaceManager.Application.Layouts;

public sealed class DesktopLayoutProtectionService : IDisposable
{
    private readonly DesktopLayoutService _desktopLayoutService;
    private readonly DisplaySettingsWatcher _displaySettingsWatcher;
    private readonly Dispatcher _dispatcher;
    private readonly object _syncRoot = new();
    private readonly TimeSpan _restoreDelay = TimeSpan.FromMilliseconds(1500);
    private CancellationTokenSource? _restoreCts;
    private DesktopLayoutSnapshot? _protectedSnapshot;
    private bool _isStarted;
    private bool _isDisposed;

    public DesktopLayoutProtectionService(
        DesktopLayoutService desktopLayoutService,
        DisplaySettingsWatcher displaySettingsWatcher,
        Dispatcher dispatcher)
    {
        _desktopLayoutService = desktopLayoutService;
        _displaySettingsWatcher = displaySettingsWatcher;
        _dispatcher = dispatcher;
    }

    public void Start()
    {
        ThrowIfDisposed();
        if (_isStarted)
        {
            return;
        }

        _displaySettingsWatcher.DisplaySettingsChanging += HandleDisplaySettingsChanging;
        _displaySettingsWatcher.DisplaySettingsChanged += HandleDisplaySettingsChanged;
        _displaySettingsWatcher.Start();
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
            _displaySettingsWatcher.DisplaySettingsChanging -= HandleDisplaySettingsChanging;
            _displaySettingsWatcher.DisplaySettingsChanged -= HandleDisplaySettingsChanged;
            _displaySettingsWatcher.Dispose();
            _isStarted = false;
        }

        CancelPendingRestore();
        _isDisposed = true;
    }

    private void HandleDisplaySettingsChanging(object? sender, EventArgs e)
    {
        RunOnDispatcher(() =>
        {
            lock (_syncRoot)
            {
                if (_protectedSnapshot is not null)
                {
                    return;
                }

                try
                {
                    _protectedSnapshot = _desktopLayoutService.Capture("显示切换保护快照");
                }
                catch
                {
                    _protectedSnapshot = null;
                }
            }
        });
    }

    private void HandleDisplaySettingsChanged(object? sender, EventArgs e)
    {
        DesktopLayoutSnapshot? protectedSnapshot;
        lock (_syncRoot)
        {
            if (_protectedSnapshot is null)
            {
                return;
            }

            protectedSnapshot = _protectedSnapshot;
            CancelPendingRestoreLocked();
            _restoreCts = new CancellationTokenSource();
        }

        _ = RestoreWhenDisplaySettlesAsync(protectedSnapshot, _restoreCts.Token);
    }

    private async Task RestoreWhenDisplaySettlesAsync(DesktopLayoutSnapshot protectedSnapshot, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(_restoreDelay, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        try
        {
            await _dispatcher.InvokeAsync(() =>
            {
                lock (_syncRoot)
                {
                    if (!ReferenceEquals(_protectedSnapshot, protectedSnapshot))
                    {
                        return;
                    }

                    var currentWidth = (int)SystemParameters.PrimaryScreenWidth;
                    var currentHeight = (int)SystemParameters.PrimaryScreenHeight;
                    if (currentWidth != protectedSnapshot.ResolutionWidth ||
                        currentHeight != protectedSnapshot.ResolutionHeight)
                    {
                        return;
                    }

                    try
                    {
                        _desktopLayoutService.Restore(protectedSnapshot);
                    }
                    catch
                    {
                        return;
                    }

                    _protectedSnapshot = null;
                    CancelPendingRestoreLocked();
                }
            });
        }
        catch (TaskCanceledException)
        {
        }
    }

    private void RunOnDispatcher(Action action)
    {
        if (_dispatcher.CheckAccess())
        {
            action();
            return;
        }

        _dispatcher.Invoke(action);
    }

    private void CancelPendingRestore()
    {
        lock (_syncRoot)
        {
            CancelPendingRestoreLocked();
        }
    }

    private void CancelPendingRestoreLocked()
    {
        _restoreCts?.Cancel();
        _restoreCts?.Dispose();
        _restoreCts = null;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }
}
