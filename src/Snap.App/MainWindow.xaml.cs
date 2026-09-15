using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Snap.App.Capture;
using Snap.App.Storage;

namespace Snap.App;

public partial class MainWindow : Window
{
    private readonly CaptureVault _vault;
    private readonly Action _beginCapture;

    public MainWindow(CaptureVault vault, Action beginCapture)
    {
        _vault = vault ?? throw new ArgumentNullException(nameof(vault));
        _beginCapture = beginCapture ?? throw new ArgumentNullException(nameof(beginCapture));
        InitializeComponent();
        DataContext = this;
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
