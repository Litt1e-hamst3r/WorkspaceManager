using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using WorkspaceManager.App;

namespace WorkspaceManager.UI.Shell;

public sealed class TrayIconHost : IDisposable
{
    private static readonly Uri AppLogoUri = new("pack://application:,,,/Assets/logol.png", UriKind.Absolute);
    private readonly MainWindow _mainWindow;
    private NotifyIcon? _notifyIcon;
    private Icon? _customTrayIcon;
    private bool _hasShownTrayHint;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(nint iconHandle);

    public TrayIconHost(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
    }

    public void Initialize()
    {
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("显示主窗口", null, (_, _) => ShowMainWindow());
        contextMenu.Items.Add("收起到托盘", null, (_, _) => HideMainWindow());
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("切换桌面图标", null, async (_, _) => await ToggleDesktopIconsAsync());
        contextMenu.Items.Add("切换任务栏", null, async (_, _) => await ToggleTaskbarAsync());
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("下一张壁纸", null, async (_, _) => await ChangeWallpaperAsync());
        contextMenu.Items.Add("收藏当前壁纸", null, (_, _) => SaveFavoriteWallpaper());
        contextMenu.Items.Add("应用默认模式", null, async (_, _) => await ApplyDefaultModeAsync());
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("退出", null, (_, _) => ExitApplication());
        _customTrayIcon = CreateTrayIcon();

        _notifyIcon = new NotifyIcon
        {
            Text = "Workspace Manager",
            Icon = _customTrayIcon ?? SystemIcons.Application,
            Visible = true,
            ContextMenuStrip = contextMenu
        };

        _notifyIcon.DoubleClick += (_, _) => ShowMainWindow();

        ShowTrayHint("应用已启动，可通过托盘直接切换桌面、任务栏和壁纸。");
    }

    public void Dispose()
    {
        if (_notifyIcon is null)
        {
            _customTrayIcon?.Dispose();
            _customTrayIcon = null;
            return;
        }

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _notifyIcon = null;
        _customTrayIcon?.Dispose();
        _customTrayIcon = null;
    }

    private async Task ToggleDesktopIconsAsync()
    {
        try
        {
            await _mainWindow.ToggleDesktopIconsFromShellAsync();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"切换桌面图标失败：{ex.Message}",
                "Workspace Manager",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void ShowMainWindow()
    {
        _mainWindow.ShowFromTray();
    }

    private void HideMainWindow()
    {
        _mainWindow.HideWindowToTrayFromShell("已从托盘收起主窗口，可通过托盘或快捷键恢复。");
    }

    private async Task ToggleTaskbarAsync()
    {
        try
        {
            await _mainWindow.ToggleTaskbarFromShellAsync();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"切换任务栏失败：{ex.Message}",
                "Workspace Manager",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private async Task ChangeWallpaperAsync()
    {
        try
        {
            await _mainWindow.ChangeWallpaperFromShellAsync();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"切换壁纸失败：{ex.Message}",
                "Workspace Manager",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void SaveFavoriteWallpaper()
    {
        try
        {
            _mainWindow.SaveFavoriteWallpaperFromShell();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"收藏壁纸失败：{ex.Message}",
                "Workspace Manager",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private async Task ApplyDefaultModeAsync()
    {
        try
        {
            await _mainWindow.ApplyDefaultModeFromShellAsync();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"应用默认模式失败：{ex.Message}",
                "Workspace Manager",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void ExitApplication()
    {
        _mainWindow.RequestExit();
        System.Windows.Application.Current.Shutdown();
    }

    private void ShowTrayHint(string message)
    {
        if (_notifyIcon is null || _hasShownTrayHint)
        {
            return;
        }

        _notifyIcon.BalloonTipTitle = "Workspace Manager";
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.ShowBalloonTip(2000);
        _hasShownTrayHint = true;
    }

    private static Icon? CreateTrayIcon()
    {
        try
        {
            var resourceInfo = System.Windows.Application.GetResourceStream(AppLogoUri);
            if (resourceInfo?.Stream is null)
            {
                return null;
            }

            using var resourceStream = resourceInfo.Stream;
            using var originalBitmap = new Bitmap(resourceStream);
            using var trayBitmap = new Bitmap(64, 64, PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(trayBitmap);

            graphics.Clear(Color.Transparent);
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            var targetBounds = GetTargetBounds(originalBitmap.Size, canvasSize: 64, padding: 6);
            graphics.DrawImage(originalBitmap, targetBounds);

            var iconHandle = trayBitmap.GetHicon();
            try
            {
                using var rawIcon = Icon.FromHandle(iconHandle);
                return (Icon)rawIcon.Clone();
            }
            finally
            {
                DestroyIcon(iconHandle);
            }
        }
        catch
        {
            return null;
        }
    }

    private static Rectangle GetTargetBounds(System.Drawing.Size sourceSize, int canvasSize, int padding)
    {
        var availableSize = Math.Max(1, canvasSize - (padding * 2));
        if (sourceSize.Width <= 0 || sourceSize.Height <= 0)
        {
            return new Rectangle(padding, padding, availableSize, availableSize);
        }

        var scale = Math.Min((float)availableSize / sourceSize.Width, (float)availableSize / sourceSize.Height);
        var width = Math.Max(1, (int)Math.Round(sourceSize.Width * scale));
        var height = Math.Max(1, (int)Math.Round(sourceSize.Height * scale));
        var x = (canvasSize - width) / 2;
        var y = (canvasSize - height) / 2;
        return new Rectangle(x, y, width, height);
    }
}
