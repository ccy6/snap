namespace Snap.Core.Capture;

public readonly record struct ArrowHead(PixelPoint LeftWing, PixelPoint RightWing);

public static class ArrowGeometry
{
    public static ArrowHead CalculateHead(
        PixelPoint start,
        PixelPoint end,
        double headLength,
        double headWidth)
    {
        var deltaX = end.X - start.X;
        var deltaY = end.Y - start.Y;
        var length = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
        if (length < 0.001)
        {
            return new ArrowHead(end, end);
        }

        var effectiveLength = Math.Min(headLength, length * 0.45);
        var effectiveWidth = headWidth * (effectiveLength / headLength);
        var unitX = deltaX / length;
        var unitY = deltaY / length;
        var baseX = end.X - (unitX * effectiveLength);
        var baseY = end.Y - (unitY * effectiveLength);
        var halfWidth = effectiveWidth / 2;
        var perpendicularX = -unitY;
        var perpendicularY = unitX;

        return new ArrowHead(
            new PixelPoint(baseX + (perpendicularX * halfWidth), baseY + (perpendicularY * halfWidth)),
            new PixelPoint(baseX - (perpendicularX * halfWidth), baseY - (perpendicularY * halfWidth)));
    }
}
