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

    public bool Remove(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var buffer = new Stack<T>();
        var removed = false;
        while (_items.TryPop(out var current))
        {
            if (!removed && EqualityComparer<T>.Default.Equals(current, item))
            {
                removed = true;
                continue;
            }

            buffer.Push(current);
        }

        while (buffer.TryPop(out var current))
        {
            _items.Push(current);
        }

        return removed;
    }
}
