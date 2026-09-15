namespace Snap.Core.Capture;

public readonly record struct ArrowHead(PixelPoint LeftWing, PixelPoint RightWing);

public sealed record ArrowPolygon(IReadOnlyList<PixelPoint> Points);

public static class ArrowGeometry
{
    public static ArrowPolygon CalculateFilledArrow(
        PixelPoint start,
        PixelPoint end,
        double shaftWidth,
        double headLength,
        double headWidth)
    {
        var deltaX = end.X - start.X;
        var deltaY = end.Y - start.Y;
        var length = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
        if (length < 0.001)
        {
            return new ArrowPolygon([start, end, end]);
        }

        var effectiveHeadLength = Math.Min(headLength, length * 0.45);
        var sizeScale = effectiveHeadLength / headLength;
        var effectiveShaftWidth = shaftWidth * Math.Max(0.55, sizeScale);
        var effectiveHeadWidth = headWidth * Math.Max(0.55, sizeScale);
        var effectiveTailWidth = effectiveShaftWidth * 0.25;
        var unitX = deltaX / length;
        var unitY = deltaY / length;
        var perpendicularX = -unitY;
        var perpendicularY = unitX;
        var baseX = end.X - (unitX * effectiveHeadLength);
        var baseY = end.Y - (unitY * effectiveHeadLength);

        PixelPoint Offset(double x, double y, double distance) =>
            new(x + (perpendicularX * distance), y + (perpendicularY * distance));

        return new ArrowPolygon(
        [
            Offset(start.X, start.Y, effectiveTailWidth / 2),
            Offset(baseX, baseY, effectiveShaftWidth / 2),
            Offset(baseX, baseY, effectiveHeadWidth / 2),
            end,
            Offset(baseX, baseY, -effectiveHeadWidth / 2),
            Offset(baseX, baseY, -effectiveShaftWidth / 2),
            Offset(start.X, start.Y, -effectiveTailWidth / 2),
        ]);
    }

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
