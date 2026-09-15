using Snap.Core.Capture;

namespace Snap.Core.Tests.Capture;

public sealed class ArrowGeometryTests
{
    [Fact]
    public void CalculateHead_ForHorizontalArrow_ReturnsSymmetricWings()
    {
        var result = ArrowGeometry.CalculateHead(
            new PixelPoint(0, 0),
            new PixelPoint(20, 0),
            headLength: 8,
            headWidth: 6);

        Assert.Equal(new PixelPoint(12, 3), result.LeftWing);
        Assert.Equal(new PixelPoint(12, -3), result.RightWing);
    }

    [Fact]
    public void CalculateHead_ForShortArrow_ScalesHeadToFit()
    {
        var result = ArrowGeometry.CalculateHead(
            new PixelPoint(0, 0),
            new PixelPoint(4, 0),
            headLength: 8,
            headWidth: 6);

        Assert.InRange(result.LeftWing.X, 2, 4);
        Assert.InRange(result.RightWing.X, 2, 4);
    }

    [Fact]
    public void CalculateHead_ForZeroLengthArrow_ReturnsEndPoint()
    {
        var end = new PixelPoint(5, 7);

        var result = ArrowGeometry.CalculateHead(end, end, 8, 6);

        Assert.Equal(end, result.LeftWing);
        Assert.Equal(end, result.RightWing);
    }
}
