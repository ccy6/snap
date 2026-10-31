using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace Snap.App.Capture;

public partial class CaptureImageWindow : Window
{
    private readonly BitmapSource _image;
    private double _zoom = 1;
    private bool _fitToWindow = true;

    public CaptureImageWindow(BitmapSource image, string createdLabel)
    {
        _image = image ?? throw new ArgumentNullException(nameof(image));
        InitializeComponent();
        PreviewImage.Source = image;
        Loaded += (_, _) => FitImage();
    }

    private void SetZoom(double zoom)
    {
        _zoom = Math.Clamp(zoom, 0.0001, 8);
        // Use pixels rather than the embedded image DPI for a predictable 1:1 view.
        var dpi = VisualTreeHelper.GetDpi(this);
        PreviewImage.Width = _image.PixelWidth * _zoom / dpi.DpiScaleX;
        PreviewImage.Height = _image.PixelHeight * _zoom / dpi.DpiScaleY;
    }

    private void FitImage()
    {
        if (ImageScroll.ActualWidth <= 0 || ImageScroll.ActualHeight <= 0) return;
        var dpi = VisualTreeHelper.GetDpi(this);
        SetZoom(Math.Min(
            Math.Max(1, ImageScroll.ActualWidth - 24) * dpi.DpiScaleX / _image.PixelWidth,
            Math.Max(1, ImageScroll.ActualHeight - 24) * dpi.DpiScaleY / _image.PixelHeight));
    }

    private void ZoomBy(double factor)
    {
        _fitToWindow = false;
        var centerX = (ImageScroll.HorizontalOffset + ImageScroll.ViewportWidth / 2) / _zoom;
        var centerY = (ImageScroll.VerticalOffset + ImageScroll.ViewportHeight / 2) / _zoom;
        SetZoom(_zoom * factor);
        ImageScroll.UpdateLayout();
        ImageScroll.ScrollToHorizontalOffset(centerX * _zoom - ImageScroll.ViewportWidth / 2);
        ImageScroll.ScrollToVerticalOffset(centerY * _zoom - ImageScroll.ViewportHeight / 2);
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
    private void OnViewportSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_fitToWindow && IsLoaded) FitImage();
    }
    private void OnImageMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            ImageScroll.ScrollToHorizontalOffset(ImageScroll.HorizontalOffset - e.Delta);
        else ZoomBy(e.Delta > 0 ? 1.25 : 0.8);
        e.Handled = true;
    }
    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { Close(); e.Handled = true; }
    }
}
