using Snap.Core.Capture;

namespace Snap.Core.Tests.Capture;

public sealed class ArrowGeometryTests
{
    [Fact]
    public void CalculateFilledArrow_ForHorizontalArrow_TapersFromTailToHead()
    {
        var result = ArrowGeometry.CalculateFilledArrow(
            new PixelPoint(0, 0),
            new PixelPoint(20, 0),
            shaftWidth: 4,
            headLength: 8,
            headWidth: 10);

        Assert.Equal(
            [
                new PixelPoint(0, 0.5),
                new PixelPoint(12, 2),
                new PixelPoint(12, 5),
                new PixelPoint(20, 0),
                new PixelPoint(12, -5),
                new PixelPoint(12, -2),
                new PixelPoint(0, -0.5),
            ],
            result.Points);
    }

    [Fact]
    public void TranslateWithinBounds_ClampsWholeArrowAndPreservesVector()
    {
        var result = ArrowGeometry.TranslateWithinBounds(
            new PixelPoint(10, 10),
            new PixelPoint(30, 20),
            deltaX: 100,
            deltaY: 100,
            new PixelRect(0, 0, 50, 40));

        Assert.Equal(new PixelPoint(30, 30), result.Start);
        Assert.Equal(new PixelPoint(50, 40), result.End);
        Assert.Equal(20, result.End.X - result.Start.X);
        Assert.Equal(10, result.End.Y - result.Start.Y);
    }

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
