using Snap.Core.Capture;

namespace Snap.Core.Tests.Capture;

public sealed class SelectionGeometryTests
{
    [Theory]
    [InlineData(10, 20, 110, 220, 10, 20, 100, 200)]
    [InlineData(110, 220, 10, 20, 10, 20, 100, 200)]
    [InlineData(10, 220, 110, 20, 10, 20, 100, 200)]
    public void FromPoints_ForAnyDragDirection_ReturnsNormalizedRectangle(
        double startX,
        double startY,
        double endX,
        double endY,
        double expectedX,
        double expectedY,
        double expectedWidth,
        double expectedHeight)
    {
        var result = SelectionGeometry.FromPoints(
            new PixelPoint(startX, startY),
            new PixelPoint(endX, endY));

        Assert.Equal(new PixelRect(expectedX, expectedY, expectedWidth, expectedHeight), result);
    }

    [Fact]
    public void Clamp_WhenRectangleExtendsPastBounds_StaysInsideBounds()
    {
        var rectangle = new PixelRect(-20, 80, 150, 80);
        var bounds = new PixelRect(0, 0, 100, 100);

        var result = SelectionGeometry.Clamp(rectangle, bounds);

        Assert.Equal(new PixelRect(0, 80, 100, 20), result);
    }
}
