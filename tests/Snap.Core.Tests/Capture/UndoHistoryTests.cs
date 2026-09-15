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

    [Fact]
    public void Remove_ExistingItem_PreservesUndoOrder()
    {
        var history = new UndoHistory<string>();
        history.Push("first");
        history.Push("selected");
        history.Push("last");

        var removed = history.Remove("selected");

        Assert.True(removed);
        Assert.Equal(2, history.Count);
        Assert.True(history.TryPop(out var last));
        Assert.Equal("last", last);
        Assert.True(history.TryPop(out var first));
        Assert.Equal("first", first);
    }
}
