using System.Runtime.InteropServices;

namespace WorkspaceManager.Interop.Desktop;

public sealed class DesktopIconService
{
    public bool IsVisible()
    {
        var handle = DesktopWindowFinder.FindDesktopListView();
        return handle != IntPtr.Zero && NativeMethods.IsWindowVisible(handle);
    }

    public Task SetVisibleAsync(bool visible, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var handle = DesktopWindowFinder.FindDesktopListView();
        if (handle == IntPtr.Zero)
        {
            throw new InvalidOperationException("无法定位桌面图标窗口。");
        }

        NativeMethods.ShowWindow(handle, visible ? NativeMethods.SW_SHOW : NativeMethods.SW_HIDE);
        return Task.CompletedTask;
    }

    public async Task ToggleAsync(CancellationToken cancellationToken = default)
    {
        await SetVisibleAsync(!IsVisible(), cancellationToken);
    }

    private static class DesktopWindowFinder
    {
        public static IntPtr FindDesktopListView()
        {
            var progman = NativeMethods.FindWindow("Progman", null);
            var shellView = FindShellView(progman);
            if (shellView != IntPtr.Zero)
            {
                return NativeMethods.FindWindowEx(shellView, IntPtr.Zero, "SysListView32", "FolderView");
            }

            var worker = IntPtr.Zero;
            while (true)
            {
                worker = NativeMethods.FindWindowEx(IntPtr.Zero, worker, "WorkerW", null);
                if (worker == IntPtr.Zero)
                {
                    break;
                }

                shellView = FindShellView(worker);
                if (shellView == IntPtr.Zero)
                {
                    continue;
                }

                var folderView = NativeMethods.FindWindowEx(shellView, IntPtr.Zero, "SysListView32", "FolderView");
                if (folderView != IntPtr.Zero)
                {
                    return folderView;
                }
            }

            return IntPtr.Zero;
        }

        private static IntPtr FindShellView(IntPtr parentHandle)
        {
            return parentHandle == IntPtr.Zero
                ? IntPtr.Zero
                : NativeMethods.FindWindowEx(parentHandle, IntPtr.Zero, "SHELLDLL_DefView", null);
        }
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
