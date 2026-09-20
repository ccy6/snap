using Snap.Core.Tray;

namespace Snap.Core.Tests.Tray;

public sealed class TrayActivationPolicyTests
{
    [Theory]
    [InlineData(false, 1, false)]
    [InlineData(true, 1, false)]
    [InlineData(false, 2, false)]
    [InlineData(true, 2, true)]
    public void ShouldOpenVault_ReturnsExpectedResult(
        bool isLeftButton,
        int clickCount,
        bool expected)
    {
        var result = TrayActivationPolicy.ShouldOpenVault(isLeftButton, clickCount);

        Assert.Equal(expected, result);
    }
}
