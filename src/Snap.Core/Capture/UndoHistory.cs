namespace Snap.Core.Capture;

public sealed class UndoHistory<T> where T : class
{
    private readonly Stack<T> _items = new();

    public int Count => _items.Count;

    public void Push(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _items.Push(item);
    }

    public bool TryPop(out T? item) => _items.TryPop(out item);
}
