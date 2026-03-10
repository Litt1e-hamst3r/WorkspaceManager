using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using WorkspaceManager.Infrastructure.Configuration;

namespace WorkspaceManager.Interop.Hotkeys;

public sealed class GlobalHotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;

    private HwndSource? _source;
    private IntPtr _windowHandle;
    private int _hotkeyId;
    private HotkeyDefinition _hotkeyDefinition;

    public GlobalHotkeyService(string? hotkeyText = null)
    {
        _hotkeyDefinition = Parse(hotkeyText);
    }

    public event EventHandler? HotkeyPressed;

    public string DisplayText => _hotkeyDefinition.DisplayText;

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
        _windowHandle = helper.Handle;
        _hotkeyId = GetHashCode();

        if (!TryRegisterCurrentHotkey())
        {
            _source.RemoveHook(WndProc);
            _source = null;
            _windowHandle = IntPtr.Zero;
            throw new InvalidOperationException("全局快捷键注册失败，可能与其他程序冲突。");
        }
    }

    public void UpdateHotkey(string? hotkeyText)
    {
        var nextDefinition = Parse(hotkeyText);
        if (nextDefinition.Equals(_hotkeyDefinition))
        {
            return;
        }

        if (_source is null)
        {
            _hotkeyDefinition = nextDefinition;
            return;
        }

        var previousDefinition = _hotkeyDefinition;

        UnregisterCurrentHotkey();
        _hotkeyDefinition = nextDefinition;

        if (TryRegisterCurrentHotkey())
        {
            return;
        }

        _hotkeyDefinition = previousDefinition;
        TryRegisterCurrentHotkey();
        throw new InvalidOperationException("新的全局快捷键注册失败，可能与其他程序冲突。");
    }

    public static string Normalize(string? hotkeyText)
    {
        return Parse(hotkeyText).DisplayText;
    }

    public static string Normalize(ModifierKeys modifiers, Key key)
    {
        return Parse(modifiers, key).DisplayText;
    }

    public void Dispose()
    {
        if (_source is null)
        {
            return;
        }

        UnregisterCurrentHotkey();
        _source.RemoveHook(WndProc);
        _source = null;
        _windowHandle = IntPtr.Zero;
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

    private bool TryRegisterCurrentHotkey()
    {
        if (_windowHandle == IntPtr.Zero)
        {
            return false;
        }

        return NativeMethods.RegisterHotKey(
            _windowHandle,
            _hotkeyId,
            _hotkeyDefinition.Modifiers,
            (uint)KeyInterop.VirtualKeyFromKey(_hotkeyDefinition.Key));
    }

    private void UnregisterCurrentHotkey()
    {
        if (_windowHandle == IntPtr.Zero)
        {
            return;
        }

        NativeMethods.UnregisterHotKey(_windowHandle, _hotkeyId);
    }

    private static HotkeyDefinition Parse(string? hotkeyText)
    {
        var text = string.IsNullOrWhiteSpace(hotkeyText)
            ? AppSettings.DefaultDesktopToggleHotkey
            : hotkeyText.Trim();

        var segments = text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length < 2)
        {
            throw new InvalidOperationException("快捷键格式无效，请使用 Ctrl+Shift+D 这样的格式。");
        }

        uint modifiers = 0;
        Key? key = null;
        var labels = new List<string>();

        foreach (var segment in segments)
        {
            if (TryParseModifier(segment, out var modifier, out var modifierLabel))
            {
                if ((modifiers & modifier) != 0)
                {
                    throw new InvalidOperationException("快捷键中包含重复的修饰键。");
                }

                modifiers |= modifier;
                labels.Add(modifierLabel);
                continue;
            }

            if (key is not null)
            {
                throw new InvalidOperationException("快捷键只能包含一个主按键。");
            }

            key = ParseKey(segment);
            labels.Add(FormatKeyLabel(key.Value));
        }

        if (modifiers == 0)
        {
            throw new InvalidOperationException("全局快捷键至少需要一个修饰键，例如 Ctrl 或 Alt。");
        }

        if (key is null)
        {
            throw new InvalidOperationException("快捷键缺少主按键。");
        }

        return new HotkeyDefinition(modifiers, key.Value, string.Join("+", labels));
    }

    private static HotkeyDefinition Parse(ModifierKeys modifiers, Key key)
    {
        var normalizedKey = NormalizeInputKey(key);
        if (IsModifierKey(normalizedKey))
        {
            throw new InvalidOperationException("请在修饰键之外再按一个主按键。");
        }

        uint nativeModifiers = 0;
        var labels = new List<string>();

        if (modifiers.HasFlag(ModifierKeys.Control))
        {
            nativeModifiers |= ModControl;
            labels.Add("Ctrl");
        }

        if (modifiers.HasFlag(ModifierKeys.Alt))
        {
            nativeModifiers |= ModAlt;
            labels.Add("Alt");
        }

        if (modifiers.HasFlag(ModifierKeys.Shift))
        {
            nativeModifiers |= ModShift;
            labels.Add("Shift");
        }

        if (modifiers.HasFlag(ModifierKeys.Windows))
        {
            nativeModifiers |= ModWin;
            labels.Add("Win");
        }

        if (nativeModifiers == 0)
        {
            throw new InvalidOperationException("全局快捷键至少需要一个修饰键，例如 Ctrl 或 Alt。");
        }

        labels.Add(FormatKeyLabel(normalizedKey));
        return new HotkeyDefinition(nativeModifiers, normalizedKey, string.Join("+", labels));
    }

    private static bool TryParseModifier(string value, out uint modifier, out string label)
    {
        switch (value.Trim().ToUpperInvariant())
        {
            case "CTRL":
            case "CONTROL":
                modifier = ModControl;
                label = "Ctrl";
                return true;
            case "ALT":
                modifier = ModAlt;
                label = "Alt";
                return true;
            case "SHIFT":
                modifier = ModShift;
                label = "Shift";
                return true;
            case "WIN":
            case "WINDOWS":
                modifier = ModWin;
                label = "Win";
                return true;
            default:
                modifier = 0;
                label = string.Empty;
                return false;
        }
    }

    private static Key ParseKey(string value)
    {
        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length == 1 && normalized[0] is >= 'A' and <= 'Z')
        {
            return (Key)Enum.Parse(typeof(Key), normalized, ignoreCase: true);
        }

        if (normalized.Length == 1 && normalized[0] is >= '0' and <= '9')
        {
            return (Key)Enum.Parse(typeof(Key), $"D{normalized}", ignoreCase: true);
        }

        return normalized switch
        {
            "SPACE" => Key.Space,
            "TAB" => Key.Tab,
            "ENTER" => Key.Enter,
            "ESC" or "ESCAPE" => Key.Escape,
            "DELETE" or "DEL" => Key.Delete,
            "INSERT" or "INS" => Key.Insert,
            "HOME" => Key.Home,
            "END" => Key.End,
            "PAGEUP" or "PGUP" => Key.PageUp,
            "PAGEDOWN" or "PGDN" => Key.PageDown,
            "UP" => Key.Up,
            "DOWN" => Key.Down,
            "LEFT" => Key.Left,
            "RIGHT" => Key.Right,
            _ when normalized.StartsWith("F", StringComparison.Ordinal)
                && int.TryParse(normalized[1..], out var functionNumber)
                && functionNumber is >= 1 and <= 24
                    => (Key)Enum.Parse(typeof(Key), normalized, ignoreCase: true),
            _ => throw new InvalidOperationException($"不支持的主按键：{value}。")
        };
    }

    private static string FormatKeyLabel(Key key)
    {
        var keyName = key.ToString();
        if (keyName.Length == 2 && keyName[0] == 'D' && char.IsDigit(keyName[1]))
        {
            return keyName[1].ToString();
        }

        return key switch
        {
            Key.PageUp => "PageUp",
            Key.PageDown => "PageDown",
            _ => keyName
        };
    }

    private static Key NormalizeInputKey(Key key)
    {
        return key switch
        {
            Key.LeftCtrl or Key.RightCtrl => Key.LeftCtrl,
            Key.LeftAlt or Key.RightAlt => Key.LeftAlt,
            Key.LeftShift or Key.RightShift => Key.LeftShift,
            Key.LWin or Key.RWin => Key.LWin,
            _ => key
        };
    }

    private static bool IsModifierKey(Key key)
    {
        return key is Key.LeftCtrl
            or Key.RightCtrl
            or Key.LeftAlt
            or Key.RightAlt
            or Key.LeftShift
            or Key.RightShift
            or Key.LWin
            or Key.RWin;
    }

    private readonly record struct HotkeyDefinition(uint Modifiers, Key Key, string DisplayText);

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
