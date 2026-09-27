namespace Snap.Core.Capture;

public static class AnnotationMovement
{
    public static PixelRect Move(PixelRect shape, double deltaX, double deltaY, PixelRect bounds) =>
        shape with
        {
            X = Math.Clamp(shape.X + deltaX, bounds.X, Math.Max(bounds.X, bounds.Right - shape.Width)),
            Y = Math.Clamp(shape.Y + deltaY, bounds.Y, Math.Max(bounds.Y, bounds.Bottom - shape.Height))
        };
}
