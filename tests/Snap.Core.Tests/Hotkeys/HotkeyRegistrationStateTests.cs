using Snap.Core.Hotkeys;

namespace Snap.Core.Tests.Hotkeys;

public sealed class HotkeyRegistrationStateTests
{
    [Fact]
    public void ReportConflict_WhileActive_EntersDormantState()
    {
        var state = new HotkeyRegistrationState();

        var changed = state.ReportConflict();

        Assert.True(changed);
        Assert.Equal(HotkeyState.Dormant, state.Current);
    }

    [Fact]
    public void ReportAvailable_WhileDormant_SilentlyReturnsToActiveState()
    {
        var state = new HotkeyRegistrationState();
        state.ReportConflict();

        var changed = state.ReportAvailable();

        Assert.True(changed);
        Assert.Equal(HotkeyState.Active, state.Current);
    }

    [Fact]
    public void ReportAvailable_WhileActive_DoesNotReportAChange()
    {
        var state = new HotkeyRegistrationState();

        var changed = state.ReportAvailable();

        Assert.False(changed);
        Assert.Equal(HotkeyState.Active, state.Current);
    }
}
