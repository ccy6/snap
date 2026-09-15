using Snap.Core.Capture;

namespace Snap.Core.Tests.Capture;

public sealed class TextSizingTests
{
    [Theory]
    [InlineData(TextSize.Small, 16)]
    [InlineData(TextSize.Medium, 24)]
    [InlineData(TextSize.Large, 32)]
    public void ToFontSize_ForEachSize_ReturnsExpectedPixels(TextSize size, double expected)
    {
        Assert.Equal(expected, TextSizing.ToFontSize(size));
    }
}
