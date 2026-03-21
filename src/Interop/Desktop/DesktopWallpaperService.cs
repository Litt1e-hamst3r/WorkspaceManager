using System.IO;
using System.Runtime.InteropServices;

namespace WorkspaceManager.Interop.Desktop;

public sealed class DesktopWallpaperService
{
    private const uint SpiSetDesktopWallpaper = 0x0014;
    private const uint SpifUpdateIniFile = 0x0001;
    private const uint SpifSendWinIniChange = 0x0002;

    public void SetWallpaper(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
        {
            throw new InvalidOperationException("壁纸文件不存在，无法设置桌面壁纸。");
        }

        var success = NativeMethods.SystemParametersInfo(
            SpiSetDesktopWallpaper,
            0,
            imagePath,
            SpifUpdateIniFile | SpifSendWinIniChange);

        if (!success)
        {
            throw new InvalidOperationException($"设置桌面壁纸失败，Win32 错误码：{Marshal.GetLastWin32Error()}。");
        }
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, string pvParam, uint fWinIni);
    }
}
