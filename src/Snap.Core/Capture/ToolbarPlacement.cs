namespace Snap.Core.Capture;

public static class ToolbarPlacement
{
    // Inputs share one coordinate system. The caller measures the visible, fitted toolbar first.
    public static PixelRect Place(PixelRect selection, double width, double height, PixelRect screen)
    {
        width = Math.Clamp(width, 0, screen.Width);
        height = Math.Clamp(height, 0, screen.Height);
        var left = Math.Clamp(selection.Right - width, screen.X, screen.Right - width);
        var below = selection.Bottom + 8;
        var above = selection.Y - height - 8;
        var top = below + height <= screen.Bottom ? below : above >= screen.Y ? above : screen.Bottom - height;
        return new PixelRect(left, Math.Clamp(top, screen.Y, screen.Bottom - height), width, height);
    }
}
