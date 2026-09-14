using Snap.Core.Hotkeys;
using Snap.Core.Settings;
using Snap.Core.Storage;

namespace Snap.Core.Tests.Settings;

public sealed class JsonSettingsStoreTests : IDisposable
{
    private readonly string _directoryPath = Path.Join(Path.GetTempPath(), $"SnapTests-{Guid.NewGuid():N}");

    [Fact]
    public async Task LoadAsync_WhenFileDoesNotExist_ReturnsDefaults()
    {
        var store = new JsonSettingsStore(Path.Join(_directoryPath, "settings.json"));

        var result = await store.LoadAsync(CancellationToken.None);

        Assert.Equal(AppSettings.CreateDefault(), result);
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsSettings()
    {
        var filePath = Path.Join(_directoryPath, "settings.json");
        var store = new JsonSettingsStore(filePath);
        var expected = new AppSettings(
            new HotkeyGesture(HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x53),
            new HotkeyGesture(HotkeyModifiers.Alt, 0x56),
            RetentionPeriod.ThirtyDays,
            StartWithWindows: false,
            TrayClickStartsCapture: false);

        await store.SaveAsync(expected, CancellationToken.None);
        var result = await store.LoadAsync(CancellationToken.None);

        Assert.Equal(expected, result);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directoryPath))
        {
            Directory.Delete(_directoryPath, recursive: true);
        }
    }
}
