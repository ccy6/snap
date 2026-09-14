using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Snap.App.Capture;

public partial class PinnedImageWindow : Window
{
    public PinnedImageWindow(BitmapSource image)
    {
        InitializeComponent();
        PinnedImage.Source = image;
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState is MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var factor = e.Delta > 0 ? 1.08 : 0.92;
        PinnedImage.MaxWidth = Math.Clamp(PinnedImage.ActualWidth * factor, 120, 2000);
        PinnedImage.MaxHeight = Math.Clamp(PinnedImage.ActualHeight * factor, 90, 1600);
    }

    private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e) => Close();
}
