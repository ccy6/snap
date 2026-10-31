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
    private bool _isRecordingHotkey;

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
        var owner = sender is MenuItem { Parent: ContextMenu { PlacementTarget: Window preview } } ? preview : this;
        if (dialog.ShowDialog(owner) is true)
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
            _vault.SetKept(capture.FilePath, !_vault.IsKept(capture.FilePath));
            RefreshCaptures();
        }
    }

    private void OnDeleteCaptureClick(object sender, RoutedEventArgs e)
    {
        if (GetCapture(sender) is { } capture)
        {
            _vault.Delete(capture.FilePath);
            if (!File.Exists(capture.FilePath))
            {
                foreach (var preview in OwnedWindows.OfType<CaptureImageWindow>()
                    .Where(window => Equals(window.Tag, capture.FilePath)).ToArray())
                    preview.Close();
            }
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
    }

    public void UpdateSettings(AppSettings settings)
    {
        _settings = settings;
        ApplySettingsToControls();
    }

    private async void OnCaptureHotkeyPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_isRecordingHotkey) return;
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape)
        {
            FinishHotkeyRecording();
            return;
        }
        if (key == Key.Tab)
        {
            FinishHotkeyRecording();
            MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            return;
        }
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
            HotkeyHint.Text = "请包含 Ctrl、Alt、Shift 或 Win，Esc 取消";
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

        _isRecordingHotkey = false;
        if (await _changeCaptureHotkey(gesture))
        {
            _settings = _settings with { CaptureHotkey = gesture };
        }

        FinishHotkeyRecording();
    }

    private void OnChangeHotkeyClick(object sender, RoutedEventArgs e)
    {
        _isRecordingHotkey = true;
        CaptureHotkeyBox.Focus();
        HotkeyHint.Text = "请按新的组合键，Esc 取消";
        CaptureHotkeyBox.SelectAll();
    }

    private void FinishHotkeyRecording()
    {
        _isRecordingHotkey = false;
        CaptureHotkeyBox.Text = _settings.CaptureHotkey.ToDisplayString();
        CaptureHotkeyLabel.Text = CaptureHotkeyBox.Text;
        HotkeyHint.Text = "全局生效 · 点击更改后录入新的组合键";
    }

    private void OnCaptureHotkeyLostFocus(object sender, KeyboardFocusChangedEventArgs e) => FinishHotkeyRecording();

    private void OnOpenCaptureClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: CapturePreview capture })
        {
            var preview = new CaptureImageWindow(LoadImage(capture.FilePath), capture.CreatedLabel)
            {
                Owner = this,
                Tag = capture.FilePath,
                ContextMenu = CreatePreviewContextMenu(capture),
            };
            preview.Show();
        }
    }

    private ContextMenu CreatePreviewContextMenu(CapturePreview capture)
    {
        var menu = new ContextMenu();
        void Add(string title, RoutedEventHandler handler)
        {
            var item = new MenuItem { Header = title, CommandParameter = capture };
            item.Click += handler;
            menu.Items.Add(item);
        }
        Add("复制", OnCopyCaptureClick);
        Add("另存为…", OnSaveCaptureClick);
        Add("钉到桌面", OnPinCaptureClick);
        menu.Items.Add(new Separator());
        Add("保留 / 取消保留", OnToggleKeepClick);
        var keepItem = (MenuItem)menu.Items[^1];
        menu.Opened += (_, _) => keepItem.Header = _vault.IsKept(capture.FilePath) ? "取消保留" : "保留";
        Add("删除", OnDeleteCaptureClick);
        return menu;
    }

    private void OnCaptureThumbnailKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Space)
        {
            e.Handled = true;
            OnOpenCaptureClick(sender, e);
        }
    }

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
        var wasInitializing = _isInitializing;
        _isInitializing = true;
        CaptureHotkeyBox.Text = _settings.CaptureHotkey.ToDisplayString();
        CaptureHotkeyLabel.Text = CaptureHotkeyBox.Text;
        RetentionComboBox.SelectedIndex = _settings.RetentionPeriod switch
        {
            RetentionPeriod.SevenDays => 1,
            RetentionPeriod.ThirtyDays => 2,
            _ => 0,
        };
        _isInitializing = wasInitializing;
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
