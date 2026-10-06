using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Snowberry.Globbing.Utilities;

/// <summary>
/// A growable list of values backed by an initial span and, once that is full, by arrays from <see cref="ArrayPool{T}.Shared"/>.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
/// <remarks>
/// Call <see cref="Dispose"/> once to return a rented array to the pool; the initial buffer is never returned. A copy
/// of the struct shares its storage, so only one copy may be used to add elements or be disposed.
/// </remarks>
internal ref struct ValueList<T>
{
    private int _count;
    private T[]? _rented;
    private Span<T> _span;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueList{T}"/> struct over <paramref name="initialBuffer"/>.
    /// </summary>
    /// <param name="initialBuffer">The storage used until it is full, typically stack-allocated.</param>
    public ValueList(Span<T> initialBuffer)
    {
        _span = initialBuffer;
        _rented = null;
        _count = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueList{T}"/> struct over an array rented from the pool.
    /// </summary>
    /// <param name="capacity">The minimum initial capacity.</param>
    public ValueList(int capacity)
    {
        _rented = ArrayPool<T>.Shared.Rent(capacity);
        _span = _rented;
        _count = 0;
    }

    /// <summary>
    /// Appends <paramref name="item"/>, growing the storage if it is full.
    /// </summary>
    /// <param name="item">The element to append.</param>
    /// <returns>The index of the appended element.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Add(in T item)
    {
        if (_count == _span.Length)
            Grow();

        _span[_count] = item;
        return _count++;
    }

    /// <summary>
    /// Gets the elements as a span.
    /// </summary>
    /// <returns>A span over the first <see cref="Count"/> elements.</returns>
    public readonly Span<T> AsSpan()
    {
        return _span[.._count];
    }

    /// <summary>
    /// Returns the rented array, if any, to the pool and resets the list to an empty list without storage.
    /// </summary>
    public void Dispose()
    {
        var rented = _rented;
        this = default;
        if (rented != null)
            ArrayPool<T>.Shared.Return(rented);
    }

    /// <summary>
    /// Removes the elements from <paramref name="count"/> onward.
    /// </summary>
    /// <param name="count">The number of elements to keep, which must not exceed <see cref="Count"/>; not validated.</param>
    public void Truncate(int count)
    {
        _count = count;
    }

    /// <summary>
    /// Replaces the storage with an array rented from the pool that has at least twice the capacity (at least 16),
    /// keeping the elements and returning the previously rented array, if any, to the pool.
    /// </summary>
    private void Grow()
    {
        var next = ArrayPool<T>.Shared.Rent(Math.Max(16, _span.Length * 2));
        _span.CopyTo(next);
        if (_rented != null)
            ArrayPool<T>.Shared.Return(_rented);

        _rented = next;
        _span = next;
    }

    /// <summary>
    /// Gets the number of elements.
    /// </summary>
    public readonly int Count => _count;

    /// <summary>
    /// Gets a reference to the element at <paramref name="index"/>.
    /// </summary>
    /// <remarks>The index is checked against the capacity, not against <see cref="Count"/>.</remarks>
    /// <param name="index">The zero-based index, which must be less than <see cref="Count"/>.</param>
    /// <returns>A reference to the element.</returns>
    public readonly ref T this[int index] => ref _span[index];
}