using System;
using System.Threading;

namespace Snowberry.Globbing.Utilities;

/// <summary>
/// Generic object pool for reducing allocations using a simple array-based stack.
/// Thread-safe for concurrent access.
/// </summary>
/// <typeparam name="T">The type of objects to pool.</typeparam>
internal class ObjectPool<T> where T : class, new()
{
    private readonly T?[] _items;
    private readonly Func<T> _objectGenerator;
    private readonly Action<T>? _resetAction;
    private int _count;

    public ObjectPool(Func<T>? objectGenerator = null, Action<T>? resetAction = null, int maxSize = 100)
    {
        _objectGenerator = objectGenerator ?? (() => new T());
        _resetAction = resetAction;
        _items = new T[maxSize];
        _count = 0;
    }

    public T Rent()
    {
        int currentCount;
        T? item;

        do
        {
            currentCount = Volatile.Read(ref _count);
            if (currentCount == 0)
                return _objectGenerator();

            int newCount = currentCount - 1;
            if (Interlocked.CompareExchange(ref _count, newCount, currentCount) == currentCount)
            {
                item = Interlocked.Exchange(ref _items[newCount], null);

                if (item != null)
                    return item;
            }
        } while (true);
    }

    public void Return(T item)
    {
        if (item == null)
            return;

        _resetAction?.Invoke(item);

        int currentCount;

        do
        {
            currentCount = Volatile.Read(ref _count);
            if (currentCount >= _items.Length)
                return;

            int newCount = currentCount + 1;
            if (Interlocked.CompareExchange(ref _count, newCount, currentCount) == currentCount)
            {
                Interlocked.Exchange(ref _items[currentCount], item);
                return;
            }
        } while (true);
    }
}

/// <summary>
/// Pooled list that can be rented and returned.
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
internal class PooledList<T> : System.Collections.Generic.List<T>
{
    public void Reset()
    {
        Clear();
    }
}
