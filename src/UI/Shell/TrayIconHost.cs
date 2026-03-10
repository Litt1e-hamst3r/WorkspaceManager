using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using WorkspaceManager.App;
using WorkspaceManager.Interop.Desktop;

namespace WorkspaceManager.UI.Shell;

public sealed class TrayIconHost : IDisposable
{
    private readonly MainWindow _mainWindow;
    private readonly DesktopIconService _desktopIconService;
    private readonly TaskbarService _taskbarService;
    private NotifyIcon? _notifyIcon;
    private bool _hasShownTrayHint;

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

        _notifyIcon = new NotifyIcon
        {
            Text = "Workspace Manager",
            Icon = SystemIcons.Application,
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
            return;
        }

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _notifyIcon = null;
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
}
