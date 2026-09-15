using Snap.Core.Capture;

namespace Snap.Core.Tests.Capture;

public sealed class UndoHistoryTests
{
    [Fact]
    public void TryPop_AfterPush_ReturnsMostRecentItem()
    {
        var history = new UndoHistory<string>();
        history.Push("first");
        history.Push("second");

        var removed = history.TryPop(out var item);

        Assert.True(removed);
        Assert.Equal("second", item);
        Assert.Equal(1, history.Count);
    }

    [Fact]
    public void TryPop_WhenEmpty_ReturnsFalse()
    {
        var history = new UndoHistory<string>();

        var removed = history.TryPop(out var item);

        Assert.False(removed);
        Assert.Null(item);
    }
}
