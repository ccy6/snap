using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Snap.App.Capture;

internal sealed class MosaicStrokeElement : FrameworkElement
{
    private readonly BitmapSource _pixelatedSource;
    private readonly GeometryGroup _strokeMask = new();
    private readonly double _radius;
    private Point? _lastPoint;

    public MosaicStrokeElement(BitmapSource pixelatedSource, double width, double height, double brushDiameter = 34)
    {
        _pixelatedSource = pixelatedSource ?? throw new ArgumentNullException(nameof(pixelatedSource));
        Width = width;
        Height = height;
        _radius = brushDiameter / 2;
        IsHitTestVisible = false;
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
    }

    public void AddPoint(Point point)
    {
        if (_lastPoint is { } previous)
        {
            var delta = point - previous;
            var distance = delta.Length;
            var step = Math.Max(3, _radius * 0.32);
            for (var travelled = step; travelled < distance; travelled += step)
            {
                var ratio = travelled / distance;
                AddMaskPoint(previous + (delta * ratio));
            }
        }

        AddMaskPoint(point);
        _lastPoint = point;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        drawingContext.PushClip(_strokeMask);
        drawingContext.DrawImage(_pixelatedSource, new Rect(0, 0, ActualWidth, ActualHeight));
        drawingContext.Pop();
    }

    private void AddMaskPoint(Point point) =>
        _strokeMask.Children.Add(new EllipseGeometry(point, _radius, _radius));
}
