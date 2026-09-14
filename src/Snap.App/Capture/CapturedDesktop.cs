using System.Windows;
using System.Windows.Media.Imaging;

namespace Snap.App.Capture;

public sealed record CapturedDesktop(BitmapSource Image, Rect VirtualBounds);
