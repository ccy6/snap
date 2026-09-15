using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Snap.App.Capture;
using Snap.App.Storage;
using Snap.Core.Hotkeys;
using Snap.Core.Settings;
using Snap.Core.Storage;

namespace Snap.App;

public partial class MainWindow : Window
{
    private readonly CaptureVault _vault;
    private readonly Action _beginCapture;
    private readonly Func<HotkeyGesture, Task<bool>> _changeCaptureHotkey;
    private readonly Func<RetentionPeriod, Task> _changeRetention;
    private AppSettings _settings;
    private bool _isInitializing;

    public MainWindow(
        CaptureVault vault,
        Action beginCapture,
        AppSettings settings,
        Func<HotkeyGesture, Task<bool>> changeCaptureHotkey,
        Func<RetentionPeriod, Task> changeRetention)
    {
        _vault = vault ?? throw new ArgumentNullException(nameof(vault));
        _beginCapture = beginCapture ?? throw new ArgumentNullException(nameof(beginCapture));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _changeCaptureHotkey = changeCaptureHotkey ?? throw new ArgumentNullException(nameof(changeCaptureHotkey));
        _changeRetention = changeRetention ?? throw new ArgumentNullException(nameof(changeRetention));
        _isInitializing = true;
        InitializeComponent();
        DataContext = this;
        ApplySettingsToControls();
        _isInitializing = false;
    }

    public ObservableCollection<CapturePreview> Captures { get; } = [];

    public void RefreshCaptures()
    {
        Captures.Clear();
        foreach (var filePath in _vault.GetCaptures())
        {
            Captures.Add(CapturePreview.Load(filePath, _vault.IsKept(filePath)));
        }
    }

    private void OnCopyCaptureClick(object sender, RoutedEventArgs e)
    {
        if (GetCapture(sender) is { } capture)
        {
            Clipboard.SetImage(LoadImage(capture.FilePath));
        }
    }

    private void OnSaveCaptureClick(object sender, RoutedEventArgs e)
    {
        if (GetCapture(sender) is not { } capture)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            FileName = Path.GetFileName(capture.FilePath),
            Filter = "PNG 图片|*.png",
        };
        if (dialog.ShowDialog(this) is true)
        {
            File.Copy(capture.FilePath, dialog.FileName, overwrite: true);
        }
    }

    private void OnPinCaptureClick(object sender, RoutedEventArgs e)
    {
        if (GetCapture(sender) is { } capture)
        {
            new PinnedImageWindow(LoadImage(capture.FilePath)).Show();
        }
    }

    private void OnToggleKeepClick(object sender, RoutedEventArgs e)
    {
        if (GetCapture(sender) is { } capture)
        {
            _vault.SetKept(capture.FilePath, !capture.IsKept);
            RefreshCaptures();
        }
    }

    private void OnDeleteCaptureClick(object sender, RoutedEventArgs e)
    {
        if (GetCapture(sender) is { } capture)
        {
            _vault.Delete(capture.FilePath);
            RefreshCaptures();
        }
    }

    private void OnStartCaptureClick(object sender, RoutedEventArgs e)
    {
        Hide();
        _beginCapture();
    }

    public void OpenSettings()
    {
        MainTabs.SelectedIndex = 1;
        CaptureHotkeyBox.Focus();
    }

    public void UpdateSettings(AppSettings settings)
    {
        _settings = settings;
        ApplySettingsToControls();
    }

    private async void OnCaptureHotkeyPreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.LeftAlt or Key.RightAlt or Key.LeftCtrl or Key.RightCtrl or
            Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
        {
            return;
        }

        var modifiers = ToHotkeyModifiers(Keyboard.Modifiers);
        var virtualKey = (uint)KeyInterop.VirtualKeyFromKey(key);
        var gesture = new HotkeyGesture(modifiers, virtualKey);
        if (!gesture.IsValid)
        {
            MessageBox.Show(this, "截图快捷键需要包含 Ctrl、Alt、Shift 或 Win。", "无法设置快捷键",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (gesture.ConflictsWithWeChatCaptureDefault &&
            MessageBox.Show(this,
                "Alt + A 是微信默认截图快捷键。微信运行时可能发生冲突，仍要设置吗？",
                "可能与微信冲突",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning) is not MessageBoxResult.OK)
        {
            CaptureHotkeyBox.Text = _settings.CaptureHotkey.ToDisplayString();
            return;
        }

        if (await _changeCaptureHotkey(gesture))
        {
            _settings = _settings with { CaptureHotkey = gesture };
        }

        CaptureHotkeyBox.Text = _settings.CaptureHotkey.ToDisplayString();
        Keyboard.ClearFocus();
    }

    private void OnCaptureHotkeyGotFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        CaptureHotkeyBox.Text = "请按组合键…";
        CaptureHotkeyBox.SelectAll();
    }

    private void OnCaptureHotkeyLostFocus(object sender, KeyboardFocusChangedEventArgs e) =>
        CaptureHotkeyBox.Text = _settings.CaptureHotkey.ToDisplayString();

    private async void OnRetentionSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        var period = RetentionComboBox.SelectedIndex switch
        {
            1 => RetentionPeriod.SevenDays,
            2 => RetentionPeriod.ThirtyDays,
            _ => RetentionPeriod.Daily,
        };
        _settings = _settings with { RetentionPeriod = period };
        await _changeRetention(period);
    }

    private void ApplySettingsToControls()
    {
        CaptureHotkeyBox.Text = _settings.CaptureHotkey.ToDisplayString();
        RetentionComboBox.SelectedIndex = _settings.RetentionPeriod switch
        {
            RetentionPeriod.SevenDays => 1,
            RetentionPeriod.ThirtyDays => 2,
            _ => 0,
        };
    }

    private static HotkeyModifiers ToHotkeyModifiers(ModifierKeys modifiers)
    {
        var result = HotkeyModifiers.None;
        if (modifiers.HasFlag(ModifierKeys.Control)) result |= HotkeyModifiers.Control;
        if (modifiers.HasFlag(ModifierKeys.Alt)) result |= HotkeyModifiers.Alt;
        if (modifiers.HasFlag(ModifierKeys.Shift)) result |= HotkeyModifiers.Shift;
        if (modifiers.HasFlag(ModifierKeys.Windows)) result |= HotkeyModifiers.Windows;
        return result;
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }

    private static CapturePreview? GetCapture(object sender) =>
        sender is MenuItem { CommandParameter: CapturePreview capture } ? capture : null;

    private static BitmapSource LoadImage(string filePath)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(filePath, UriKind.Absolute);
        image.EndInit();
        image.Freeze();
        return image;
    }
}
