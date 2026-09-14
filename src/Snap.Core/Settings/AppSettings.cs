using Snap.Core.Hotkeys;
using Snap.Core.Storage;

namespace Snap.Core.Settings;

public sealed record AppSettings(
    HotkeyGesture CaptureHotkey,
    HotkeyGesture? VaultHotkey,
    RetentionPeriod RetentionPeriod,
    bool StartWithWindows,
    bool TrayClickStartsCapture)
{
    public static AppSettings CreateDefault() => new(
        new HotkeyGesture(HotkeyModifiers.Alt, 0x51),
        VaultHotkey: null,
        RetentionPeriod.Daily,
        StartWithWindows: true,
        TrayClickStartsCapture: true);
}
