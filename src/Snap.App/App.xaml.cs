using System.Drawing;
using System.IO;
using System.Windows;
using Forms = System.Windows.Forms;
using Snap.App.Capture;
using Snap.App.Hotkeys;
using Snap.App.Storage;
using Snap.Core.Settings;
using Snap.Core.Hotkeys;
using Snap.Core.Storage;
using Snap.Core.Tray;

namespace Snap.App;

public partial class App : System.Windows.Application
{
    private Forms.NotifyIcon? _notifyIcon;
    private GlobalHotkeyService? _hotkeyService;
    private MainWindow? _mainWindow;
    private CaptureOverlayWindow? _captureWindow;
    private CaptureVault? _vault;
    private JsonSettingsStore? _settingsStore;
    private AppSettings _settings = AppSettings.CreateDefault();

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var appDataDirectory = Path.Join(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Snap");
        _settingsStore = new JsonSettingsStore(Path.Join(appDataDirectory, "settings.json"));
        _settings = await _settingsStore.LoadAsync();
        _vault = new CaptureVault(Path.Join(appDataDirectory, "Captures"));
        _vault.Cleanup(_settings.RetentionPeriod);

        _hotkeyService = new GlobalHotkeyService();
        _hotkeyService.Pressed += OnCaptureHotkeyPressed;
        _mainWindow = new MainWindow(
            _vault,
            BeginCapture,
            _settings,
            ChangeCaptureHotkeyAsync,
            ChangeRetentionAsync);

        if (!_hotkeyService.Register(_settings.CaptureHotkey) &&
            !ResolveHotkeyConflict(_settings.CaptureHotkey, canRestorePrevious: false))
        {
            return;
        }

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("开始截图", null, (_, _) => BeginCapture());
        menu.Items.Add("截图台", null, (_, _) => ShowVault());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("退出 Snap", null, (_, _) => Shutdown());

        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = LoadAppIcon(),
            Text = "Snap 截图工具",
            Visible = true,
            ContextMenuStrip = menu,
        };
        _notifyIcon.MouseDoubleClick += OnTrayMouseDoubleClick;
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

    private void OnTrayMouseDoubleClick(object? sender, Forms.MouseEventArgs e)
    {
        if (TrayActivationPolicy.ShouldOpenVault(
                e.Button is Forms.MouseButtons.Left,
                e.Clicks))
        {
            ShowVault();
        }
    }

    private static Icon LoadAppIcon()
    {
        var resource = GetResourceStream(new Uri("pack://application:,,,/Assets/Snap.ico"))
            ?? throw new InvalidOperationException("The Snap application icon is missing.");
        using var stream = resource.Stream;
        using var icon = new Icon(stream);
        return (Icon)icon.Clone();
    }

    private void BeginCapture()
    {
        if (_captureWindow is not null)
        {
            _captureWindow.Activate();
            return;
        }

        if (_vault is null)
        {
            return;
        }

        _captureWindow = new CaptureOverlayWindow(
            new ScreenCaptureService(),
            new WindowSelectionService(),
            _vault);
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

    private async Task<bool> ChangeCaptureHotkeyAsync(HotkeyGesture hotkey)
    {
        if (_hotkeyService is null || _settingsStore is null)
        {
            return false;
        }

        var previousHotkey = _settings.CaptureHotkey;
        if (!_hotkeyService.Register(hotkey))
        {
            return ResolveHotkeyConflict(hotkey, canRestorePrevious: true, previousHotkey);
        }

        _settings = _settings with { CaptureHotkey = hotkey };
        await _settingsStore.SaveAsync(_settings);
        _mainWindow?.UpdateSettings(_settings);
        return true;
    }

    private async Task ChangeRetentionAsync(RetentionPeriod period)
    {
        if (_settingsStore is null)
        {
            return;
        }

        _settings = _settings with { RetentionPeriod = period };
        await _settingsStore.SaveAsync(_settings);
        _vault?.Cleanup(period);
        _mainWindow?.RefreshCaptures();
    }

    private bool ResolveHotkeyConflict(
        HotkeyGesture hotkey,
        bool canRestorePrevious,
        HotkeyGesture? previousHotkey = null)
    {
        var dialog = new HotkeyConflictDialog(hotkey);
        dialog.ShowDialog();
        switch (dialog.Choice)
        {
            case HotkeyConflictChoice.CloseSnap:
                Shutdown();
                return false;

            case HotkeyConflictChoice.ChangeHotkey:
                if (canRestorePrevious && previousHotkey is not null)
                {
                    _hotkeyService?.Register(previousHotkey);
                }

                ShowVault();
                _mainWindow?.OpenSettings();
                return !canRestorePrevious;

            default:
                return false;
        }
    }
}
