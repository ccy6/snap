using Snap.Core.Capture;

namespace Snap.Core.Tests.Capture;

public sealed class SelectionAdjustmentTests
{
    private static readonly PixelRect Bounds = new(0, 0, 100, 80);

    [Fact]
    public void Move_ClampsWithoutChangingSize()
    {
        var selection = new PixelRect(70, 50, 20, 20);

        var result = SelectionAdjustment.Move(selection, 50, 50, Bounds);

        Assert.Equal(new PixelRect(80, 60, 20, 20), result);
    }

    [Fact]
    public void ResizeTopLeft_MovesOnlyRequestedEdges()
    {
        var selection = new PixelRect(20, 20, 40, 30);

        var result = SelectionAdjustment.Resize(
            selection,
            SelectionHandle.TopLeft,
            deltaX: -10,
            deltaY: 5,
            Bounds);

        Assert.Equal(new PixelRect(10, 25, 50, 25), result);
    }

    [Fact]
    public void ResizeRight_EnforcesMinimumSize()
    {
        var selection = new PixelRect(20, 20, 40, 30);

        var result = SelectionAdjustment.Resize(
            selection,
            SelectionHandle.Right,
            deltaX: -100,
            deltaY: 0,
            Bounds,
            minimumSize: 8);

        Assert.Equal(new PixelRect(20, 20, 8, 30), result);
    }
}
