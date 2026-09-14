namespace Snap.Core.Capture;

public static class SelectionGeometry
{
    public static PixelRect FromPoints(PixelPoint start, PixelPoint end)
    {
        var left = Math.Min(start.X, end.X);
        var top = Math.Min(start.Y, end.Y);
        return new PixelRect(left, top, Math.Abs(end.X - start.X), Math.Abs(end.Y - start.Y));
    }

    public static PixelRect Clamp(PixelRect rectangle, PixelRect bounds)
    {
        var left = Math.Max(rectangle.X, bounds.X);
        var top = Math.Max(rectangle.Y, bounds.Y);
        var right = Math.Min(rectangle.Right, bounds.Right);
        var bottom = Math.Min(rectangle.Bottom, bounds.Bottom);

        return new PixelRect(
            left,
            top,
            Math.Max(0, right - left),
            Math.Max(0, bottom - top));
    }
}
