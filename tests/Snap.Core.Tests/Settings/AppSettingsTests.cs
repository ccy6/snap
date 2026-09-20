using Snap.Core.Hotkeys;
using Snap.Core.Settings;
using Snap.Core.Storage;

namespace Snap.Core.Tests.Settings;

public sealed class AppSettingsTests
{
    [Fact]
    public void CreateDefault_ReturnsApprovedDefaults()
    {
        var settings = AppSettings.CreateDefault();

        Assert.Multiple(
            () => Assert.Equal(new HotkeyGesture(HotkeyModifiers.Alt, 0x51), settings.CaptureHotkey),
            () => Assert.Null(settings.VaultHotkey),
            () => Assert.Equal(RetentionPeriod.Daily, settings.RetentionPeriod),
            () => Assert.True(settings.StartWithWindows));
    }

    [Fact]
    public void AppSettings_DoesNotExposeLegacyTrayClickSetting()
    {
        Assert.Null(typeof(AppSettings).GetProperty("TrayClickStartsCapture"));
    }
}
