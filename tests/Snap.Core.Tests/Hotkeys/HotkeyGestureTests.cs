using Snap.Core.Hotkeys;

namespace Snap.Core.Tests.Hotkeys;

public sealed class HotkeyGestureTests
{
    [Fact]
    public void ToDisplayString_UsesStableModifierOrder()
    {
        var gesture = new HotkeyGesture(
            HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift,
            0x51);

        Assert.Equal("Ctrl + Alt + Shift + Q", gesture.ToDisplayString());
    }

    [Theory]
    [InlineData(HotkeyModifiers.Alt, 0x41, true)]
    [InlineData(HotkeyModifiers.Alt, 0x51, false)]
    [InlineData(HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x41, false)]
    public void ConflictsWithWeChatCaptureDefault_OnlyMatchesAltA(
        HotkeyModifiers modifiers,
        uint virtualKey,
        bool expected)
    {
        var gesture = new HotkeyGesture(modifiers, virtualKey);

        Assert.Equal(expected, gesture.ConflictsWithWeChatCaptureDefault);
    }

    [Theory]
    [InlineData(HotkeyModifiers.None, false)]
    [InlineData(HotkeyModifiers.Alt, true)]
    [InlineData(HotkeyModifiers.Control, true)]
    [InlineData(HotkeyModifiers.Shift, true)]
    [InlineData(HotkeyModifiers.Windows, true)]
    public void IsValid_RequiresAtLeastOneModifier(HotkeyModifiers modifiers, bool expected)
    {
        var gesture = new HotkeyGesture(modifiers, 0x51);

        Assert.Equal(expected, gesture.IsValid);
    }
}
