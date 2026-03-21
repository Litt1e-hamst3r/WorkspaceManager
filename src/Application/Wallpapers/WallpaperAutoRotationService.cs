namespace WorkspaceManager.Application.Wallpapers;

public sealed class WallpaperAutoRotationService : IDisposable
{
    private readonly object _syncRoot = new();
    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private bool _isDisposed;

    public event Func<Task>? RotationRequested;

    public bool IsEnabled { get; private set; }

    public TimeSpan Interval { get; private set; }

    public void Configure(bool enabled, TimeSpan interval)
    {
        ThrowIfDisposed();
        if (!enabled)
        {
            StopLoop();
            IsEnabled = false;
            Interval = TimeSpan.Zero;
            return;
        }

        if (interval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("壁纸轮换间隔必须大于 0。");
        }

        StopLoop();
        IsEnabled = true;
        Interval = interval;

        var cts = new CancellationTokenSource();
        lock (_syncRoot)
        {
            _cts = cts;
            _loopTask = RunLoopAsync(interval, cts.Token);
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        StopLoop();
        _isDisposed = true;
    }

    private async Task RunLoopAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(interval);
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                var handlers = RotationRequested;
                if (handlers is null)
                {
                    continue;
                }

                foreach (Func<Task> handler in handlers.GetInvocationList())
                {
                    try
                    {
                        await handler();
                    }
                    catch
                    {
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void StopLoop()
    {
        CancellationTokenSource? cts;
        lock (_syncRoot)
        {
            cts = _cts;
            _cts = null;
            _loopTask = null;
        }

        if (cts is null)
        {
            return;
        }

        try
        {
            cts.Cancel();
        }
        catch
        {
        }
        finally
        {
            cts.Dispose();
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }
}
