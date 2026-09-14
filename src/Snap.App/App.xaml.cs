using System.Drawing;
using System.IO;
using System.Windows;
using Forms = System.Windows.Forms;
using Snap.App.Capture;
using Snap.App.Hotkeys;
using Snap.App.Storage;
using Snap.Core.Settings;

namespace Snap.App;

public partial class App : System.Windows.Application
{
    private Forms.NotifyIcon? _notifyIcon;
    private GlobalHotkeyService? _hotkeyService;
    private MainWindow? _mainWindow;
    private CaptureOverlayWindow? _captureWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var captureDirectory = Path.Join(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Snap",
            "Captures");
        var settings = AppSettings.CreateDefault();
        var vault = new CaptureVault(captureDirectory);

        _mainWindow = new MainWindow(vault, BeginCapture);
        _hotkeyService = new GlobalHotkeyService();
        _hotkeyService.Pressed += OnCaptureHotkeyPressed;
        _hotkeyService.Register(settings.CaptureHotkey);

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("开始截图", null, (_, _) => BeginCapture());
        menu.Items.Add("截图台", null, (_, _) => ShowVault());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("退出 Snap", null, (_, _) => Shutdown());

        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "Snap 截图工具",
            Visible = true,
            ContextMenuStrip = menu,
        };
        _notifyIcon.MouseClick += OnTrayMouseClick;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_notifyIcon is not null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }

        _hotkeyService?.Dispose();
        base.OnExit(e);
    }

    private void OnCaptureHotkeyPressed(object? sender, EventArgs e) => BeginCapture();

    private void OnTrayMouseClick(object? sender, Forms.MouseEventArgs e)
    {
        if (e.Button is Forms.MouseButtons.Left)
        {
            BeginCapture();
        }
    }

    private void BeginCapture()
    {
        if (_captureWindow is not null)
        {
            _captureWindow.Activate();
            return;
        }

        var captureDirectory = Path.Join(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Snap",
            "Captures");
        _captureWindow = new CaptureOverlayWindow(new ScreenCaptureService(), new CaptureVault(captureDirectory));
        _captureWindow.Closed += (_, _) => _captureWindow = null;
        _captureWindow.Show();
    }

    private void ShowVault()
    {
        if (_mainWindow is null)
        {
            return;
        }

        _mainWindow.RefreshCaptures();
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }
}
