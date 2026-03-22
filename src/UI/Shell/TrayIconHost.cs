using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using WorkspaceManager.App;
using WorkspaceManager.Interop.Desktop;

namespace WorkspaceManager.UI.Shell;

public sealed class TrayIconHost : IDisposable
{
    private static readonly Uri AppLogoUri = new("pack://application:,,,/Assets/logol.png", UriKind.Absolute);
    private readonly MainWindow _mainWindow;
    private readonly DesktopIconService _desktopIconService;
    private readonly TaskbarService _taskbarService;
    private NotifyIcon? _notifyIcon;
    private Icon? _customTrayIcon;
    private bool _hasShownTrayHint;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(nint iconHandle);

    public TrayIconHost(MainWindow mainWindow, DesktopIconService desktopIconService, TaskbarService taskbarService)
    {
        _mainWindow = mainWindow;
        _desktopIconService = desktopIconService;
        _taskbarService = taskbarService;
    }

    public void Initialize()
    {
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("切换桌面图标", null, async (_, _) => await ToggleDesktopIconsAsync());
        contextMenu.Items.Add("切换任务栏", null, async (_, _) => await ToggleTaskbarAsync());
        contextMenu.Items.Add("显示主窗口", null, (_, _) => ShowMainWindow());
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

        ShowTrayHint("应用已启动，可最小化或关闭到托盘继续运行。");
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
            await _desktopIconService.ToggleAsync();
            _mainWindow.Dispatcher.Invoke(_mainWindow.RefreshDesktopIconState);
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

    private async Task ToggleTaskbarAsync()
    {
        try
        {
            await _taskbarService.ToggleAsync();
            _mainWindow.Dispatcher.Invoke(_mainWindow.RefreshTaskbarState);
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
