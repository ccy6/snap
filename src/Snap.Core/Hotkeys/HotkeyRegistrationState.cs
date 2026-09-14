namespace Snap.Core.Hotkeys;

public sealed class HotkeyRegistrationState
{
    public HotkeyState Current { get; private set; } = HotkeyState.Active;

    public bool ReportConflict()
    {
        if (Current is HotkeyState.Dormant)
        {
            return false;
        }

        Current = HotkeyState.Dormant;
        return true;
    }

    public bool ReportAvailable()
    {
        if (Current is HotkeyState.Active)
        {
            return false;
        }

        Current = HotkeyState.Active;
        return true;
    }
}
