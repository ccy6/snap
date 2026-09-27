using Snap.Core.Capture;

namespace Snap.Core.Tests.Capture;

public sealed class ToolbarPlacementTests
{
    [Fact]
    public void BottomEdge_MovesBothRowsAboveSelection()
    {
        var result = ToolbarPlacement.Place(new(700, 650, 250, 110), 640, 90, new(6, 6, 988, 748));
        Assert.Equal(552, result.Y);
        Assert.True(result.Bottom <= 650);
    }

    [Fact]
    public void FullScreen_KeepsWholeToolbarInsideWorkArea()
    {
        var result = ToolbarPlacement.Place(new(0, 0, 1920, 1080), 640, 90, new(6, 6, 1908, 1028));
        Assert.Equal(1034, result.Bottom);
        Assert.Equal(1914, result.Right);
    }

    [Theory]
    [InlineData(-1920, 100, 1280, 720)]
    [InlineData(1920, 400, 800, 600)]
    [InlineData(0, 0, 320, 240)]
    public void DifferentMonitorOriginsAndSizes_KeepBothRowsVisible(double x, double y, double w, double h)
    {
        var screen = new PixelRect(x, y, w, h);
        foreach (var selection in new[] { new PixelRect(x, y, 10, 10), screen, new PixelRect(x + w - 10, y + h - 10, 10, 10) })
        {
            var result = ToolbarPlacement.Place(selection, Math.Min(640, w), 90, screen);
            Assert.InRange(result.X, screen.X, screen.Right - result.Width);
            Assert.InRange(result.Y, screen.Y, screen.Bottom - result.Height);
        }
    }
}
