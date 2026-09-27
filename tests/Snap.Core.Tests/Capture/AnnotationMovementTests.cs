using Snap.Core.Capture;

namespace Snap.Core.Tests.Capture;

public sealed class AnnotationMovementTests
{
    [Theory]
    [InlineData(20, 10, 50, 30)]
    [InlineData(-100, -100, 0, 0)]
    [InlineData(1000, 1000, 160, 120)]
    public void Drag_PreservesDimensionsAndClampsAtCaptureEdges(double dx, double dy, double x, double y)
    {
        var result = AnnotationMovement.Move(new(30, 20, 40, 30), dx, dy, new(0, 0, 200, 150));
        Assert.Equal(new PixelRect(x, y, 40, 30), result);
    }

    [Fact]
    public void CaptureShrunkBelowShapeSize_DragDoesNotThrowOrResizeShape()
    {
        Assert.Equal(new PixelRect(0, 0, 100, 80), AnnotationMovement.Move(new(10, 10, 100, 80), 5, 5, new(0, 0, 30, 20)));
    }
}
