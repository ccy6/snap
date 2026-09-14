using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Snap.App.Storage;
using Snap.Core.Capture;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfPoint = System.Windows.Point;

namespace Snap.App.Capture;

public partial class CaptureOverlayWindow : Window
{
    private readonly CaptureVault _vault;
    private readonly CapturedDesktop _desktop;
    private WpfPoint _startPoint;
    private Rect _selection;
    private bool _isSelecting;

    public CaptureOverlayWindow(ScreenCaptureService captureService, CaptureVault vault)
    {
        ArgumentNullException.ThrowIfNull(captureService);
        _vault = vault ?? throw new ArgumentNullException(nameof(vault));
        _desktop = captureService.Capture();

        InitializeComponent();
        DesktopImage.Source = _desktop.Image;
        Left = _desktop.VirtualBounds.Left;
        Top = _desktop.VirtualBounds.Top;
        Width = _desktop.VirtualBounds.Width;
        Height = _desktop.VirtualBounds.Height;
        Loaded += (_, _) => Activate();
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && !_selection.IsEmpty && _selection.Contains(e.GetPosition(OverlayCanvas)))
        {
            CompleteCapture();
            return;
        }

        if (FindParent<ButtonBase>(e.OriginalSource as DependencyObject) is not null)
        {
            return;
        }

        _startPoint = e.GetPosition(OverlayCanvas);
        _selection = Rect.Empty;
        _isSelecting = true;
        ActionBar.Visibility = Visibility.Collapsed;
        SelectionBorder.Visibility = Visibility.Visible;
        SizeBadge.Visibility = Visibility.Visible;
        OverlayCanvas.CaptureMouse();
        UpdateSelection(_startPoint);
    }

    private void OnMouseMove(object sender, WpfMouseEventArgs e)
    {
        if (_isSelecting)
        {
            UpdateSelection(e.GetPosition(OverlayCanvas));
        }
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isSelecting)
        {
            return;
        }

        _isSelecting = false;
        OverlayCanvas.ReleaseMouseCapture();
        UpdateSelection(e.GetPosition(OverlayCanvas));
        if (_selection.Width < 2 || _selection.Height < 2)
        {
            ResetSelection();
            return;
        }

        PositionActionBar();
        ActionBar.Visibility = Visibility.Visible;
    }

    private void OnKeyDown(object sender, WpfKeyEventArgs e)
    {
        if (e.Key is Key.Escape)
        {
            Close();
        }
        else if (e.Key is Key.Enter && !_selection.IsEmpty)
        {
            CompleteCapture();
        }
    }

    private void OnDoneClick(object sender, RoutedEventArgs e) => CompleteCapture();

    private void OnCancelClick(object sender, RoutedEventArgs e) => Close();

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        var image = CreateCrop();
        if (image is null)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            FileName = $"Snap_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png",
            DefaultExt = ".png",
            Filter = "PNG 图片|*.png|JPEG 图片|*.jpg;*.jpeg",
        };
        if (dialog.ShowDialog(this) is true)
        {
            SaveImage(image, dialog.FileName);
            _vault.Save(image);
            Close();
        }
    }

    private void OnPinClick(object sender, RoutedEventArgs e)
    {
        var image = CreateCrop();
        if (image is null)
        {
            return;
        }

        _vault.Save(image);
        new PinnedImageWindow(image).Show();
        Close();
    }

    private void CompleteCapture()
    {
        var image = CreateCrop();
        if (image is null)
        {
            return;
        }

        Clipboard.SetImage(image);
        _vault.Save(image);
        Close();
    }

    private CroppedBitmap? CreateCrop()
    {
        if (_selection.IsEmpty || ActualWidth <= 0 || ActualHeight <= 0)
        {
            return null;
        }

        var scaleX = _desktop.Image.PixelWidth / ActualWidth;
        var scaleY = _desktop.Image.PixelHeight / ActualHeight;
        var source = new Int32Rect(
            (int)Math.Floor(_selection.X * scaleX),
            (int)Math.Floor(_selection.Y * scaleY),
            Math.Max(1, (int)Math.Round(_selection.Width * scaleX)),
            Math.Max(1, (int)Math.Round(_selection.Height * scaleY)));
        source.Width = Math.Min(source.Width, _desktop.Image.PixelWidth - source.X);
        source.Height = Math.Min(source.Height, _desktop.Image.PixelHeight - source.Y);

        var crop = new CroppedBitmap(_desktop.Image, source);
        crop.Freeze();
        return crop;
    }

    private static void SaveImage(BitmapSource image, string filePath)
    {
        BitmapEncoder encoder = Path.GetExtension(filePath).Equals(".png", StringComparison.OrdinalIgnoreCase)
            ? new PngBitmapEncoder()
            : new JpegBitmapEncoder { QualityLevel = 95 };
        encoder.Frames.Add(BitmapFrame.Create(image));
        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
        encoder.Save(stream);
    }

    private void UpdateSelection(WpfPoint currentPoint)
    {
        var normalized = SelectionGeometry.FromPoints(
            new PixelPoint(_startPoint.X, _startPoint.Y),
            new PixelPoint(currentPoint.X, currentPoint.Y));
        var clamped = SelectionGeometry.Clamp(
            normalized,
            new PixelRect(0, 0, OverlayCanvas.ActualWidth, OverlayCanvas.ActualHeight));
        _selection = new Rect(clamped.X, clamped.Y, clamped.Width, clamped.Height);

        SetBounds(SelectionBorder, _selection.X, _selection.Y, _selection.Width, _selection.Height);
        SizeText.Text = $"{Math.Round(_selection.Width):0} × {Math.Round(_selection.Height):0}";
        SizeBadge.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Canvas.SetLeft(SizeBadge, _selection.X);
        Canvas.SetTop(SizeBadge, Math.Max(0, _selection.Y - SizeBadge.DesiredSize.Height - 5));
        UpdateShade();
    }

    private void UpdateShade()
    {
        SetBounds(TopShade, 0, 0, OverlayCanvas.ActualWidth, _selection.Y);
        SetBounds(LeftShade, 0, _selection.Y, _selection.X, _selection.Height);
        SetBounds(
            RightShade,
            _selection.Right,
            _selection.Y,
            Math.Max(0, OverlayCanvas.ActualWidth - _selection.Right),
            _selection.Height);
        SetBounds(
            BottomShade,
            0,
            _selection.Bottom,
            OverlayCanvas.ActualWidth,
            Math.Max(0, OverlayCanvas.ActualHeight - _selection.Bottom));
    }

    private void PositionActionBar()
    {
        ActionBar.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var width = ActionBar.DesiredSize.Width;
        var height = ActionBar.DesiredSize.Height;
        var left = Math.Clamp(_selection.Right - width, 0, Math.Max(0, OverlayCanvas.ActualWidth - width));
        var preferredTop = _selection.Bottom + 8;
        var top = preferredTop + height <= OverlayCanvas.ActualHeight
            ? preferredTop
            : Math.Max(0, _selection.Y - height - 8);
        Canvas.SetLeft(ActionBar, left);
        Canvas.SetTop(ActionBar, top);
    }

    private void ResetSelection()
    {
        _selection = Rect.Empty;
        SelectionBorder.Visibility = Visibility.Collapsed;
        SizeBadge.Visibility = Visibility.Collapsed;
        ActionBar.Visibility = Visibility.Collapsed;
        SetBounds(TopShade, 0, 0, OverlayCanvas.ActualWidth, OverlayCanvas.ActualHeight);
        SetBounds(LeftShade, 0, 0, 0, 0);
        SetBounds(RightShade, 0, 0, 0, 0);
        SetBounds(BottomShade, 0, 0, 0, 0);
    }

    private static void SetBounds(FrameworkElement element, double left, double top, double width, double height)
    {
        Canvas.SetLeft(element, left);
        Canvas.SetTop(element, top);
        element.Width = Math.Max(0, width);
        element.Height = Math.Max(0, height);
    }

    private static T? FindParent<T>(DependencyObject? source) where T : DependencyObject
    {
        while (source is not null)
        {
            if (source is T match)
            {
                return match;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return null;
    }
}
