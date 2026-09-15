namespace Snap.Core.Capture;

public static class SelectionAdjustment
{
    public static PixelRect Move(PixelRect selection, double deltaX, double deltaY, PixelRect bounds)
    {
        var x = Math.Clamp(selection.X + deltaX, bounds.X, bounds.Right - selection.Width);
        var y = Math.Clamp(selection.Y + deltaY, bounds.Y, bounds.Bottom - selection.Height);
        return selection with { X = x, Y = y };
    }

    public static PixelRect Resize(
        PixelRect selection,
        SelectionHandle handle,
        double deltaX,
        double deltaY,
        PixelRect bounds,
        double minimumSize = 2)
    {
        var left = selection.X;
        var top = selection.Y;
        var right = selection.Right;
        var bottom = selection.Bottom;

        if (handle is SelectionHandle.TopLeft or SelectionHandle.Left or SelectionHandle.BottomLeft)
        {
            left = Math.Clamp(left + deltaX, bounds.X, right - minimumSize);
        }

        if (handle is SelectionHandle.TopRight or SelectionHandle.Right or SelectionHandle.BottomRight)
        {
            right = Math.Clamp(right + deltaX, left + minimumSize, bounds.Right);
        }

        if (handle is SelectionHandle.TopLeft or SelectionHandle.Top or SelectionHandle.TopRight)
        {
            top = Math.Clamp(top + deltaY, bounds.Y, bottom - minimumSize);
        }

        if (handle is SelectionHandle.BottomLeft or SelectionHandle.Bottom or SelectionHandle.BottomRight)
        {
            bottom = Math.Clamp(bottom + deltaY, top + minimumSize, bounds.Bottom);
        }

        return new PixelRect(left, top, right - left, bottom - top);
    }
}
