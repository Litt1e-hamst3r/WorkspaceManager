using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace WorkspaceManager.App;

public sealed class GlobalHotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;

    private HwndSource? _source;
    private int _hotkeyId;

    public event EventHandler? HotkeyPressed;

    public string DisplayText => "Ctrl+Shift+D";

    public void Register(Window window)
    {
        var helper = new WindowInteropHelper(window);
        if (helper.Handle == IntPtr.Zero)
        {
            throw new InvalidOperationException("窗口句柄尚未初始化，无法注册全局快捷键。");
        }

        _source = HwndSource.FromHwnd(helper.Handle)
            ?? throw new InvalidOperationException("无法获取窗口消息源。");

        _source.AddHook(WndProc);
        _hotkeyId = GetHashCode();

        if (!NativeMethods.RegisterHotKey(helper.Handle, _hotkeyId, ModControl | ModShift, (uint)KeyInterop.VirtualKeyFromKey(Key.D)))
        {
            _source.RemoveHook(WndProc);
            _source = null;
            throw new InvalidOperationException("全局快捷键注册失败，可能与其他程序冲突。");
        }
    }

    public void Dispose()
    {
        if (_source is null)
        {
            return;
        }

        NativeMethods.UnregisterHotKey(_source.Handle, _hotkeyId);
        _source.RemoveHook(WndProc);
        _source = null;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == _hotkeyId)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
            handled = true;
        }

        return IntPtr.Zero;
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
