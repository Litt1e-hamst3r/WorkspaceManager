using System.Runtime.InteropServices;

namespace WorkspaceManager.App;

public sealed class TaskbarService
{
    public bool IsVisible()
    {
        var handles = FindTaskbarHandles();
        return handles.Any(handle => NativeMethods.IsWindowVisible(handle));
    }

    public Task SetVisibleAsync(bool visible, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var handles = FindTaskbarHandles();
        if (handles.Count == 0)
        {
            throw new InvalidOperationException("无法定位任务栏窗口。");
        }

        foreach (var handle in handles)
        {
            NativeMethods.ShowWindow(handle, visible ? NativeMethods.SW_SHOW : NativeMethods.SW_HIDE);
        }

        return Task.CompletedTask;
    }

    public async Task ToggleAsync(CancellationToken cancellationToken = default)
    {
        await SetVisibleAsync(!IsVisible(), cancellationToken);
    }

    private static List<IntPtr> FindTaskbarHandles()
    {
        var handles = new List<IntPtr>();

        var primaryTaskbar = NativeMethods.FindWindow("Shell_TrayWnd", null);
        if (primaryTaskbar != IntPtr.Zero)
        {
            handles.Add(primaryTaskbar);
        }

        var current = IntPtr.Zero;
        while (true)
        {
            current = NativeMethods.FindWindowEx(IntPtr.Zero, current, "Shell_SecondaryTrayWnd", null);
            if (current == IntPtr.Zero)
            {
                break;
            }

            handles.Add(current);
        }

        return handles;
    }

    private static class NativeMethods
    {
        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr FindWindow(string? className, string? windowName);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr FindWindowEx(
            IntPtr parentHandle,
            IntPtr childAfter,
            string? className,
            string? windowTitle);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(IntPtr windowHandle, int command);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr windowHandle);
    }
}
