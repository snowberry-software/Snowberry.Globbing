using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Snowberry.Globbing.Utilities;

/// <summary>
/// A string builder backed by an initial span and, once that is full, by arrays from <see cref="ArrayPool{T}.Shared"/>.
/// </summary>
/// <remarks>
/// Finish with either <see cref="ToString"/> or <see cref="Dispose"/>; both return a rented array to the pool and reset
/// the builder to an empty builder without storage. The initial buffer is never returned. A copy of the struct shares its
/// storage, so only one copy may be used to append or be finished.
/// </remarks>
internal ref struct ValueStringBuilder
{
    private Span<char> _chars;
    private int _length;
    private char[]? _rented;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueStringBuilder"/> struct over <paramref name="initialBuffer"/>.
    /// </summary>
    /// <param name="initialBuffer">The storage used until it is full, typically stack-allocated.</param>
    public ValueStringBuilder(Span<char> initialBuffer)
    {
        _chars = initialBuffer;
        _rented = null;
        _length = 0;
    }

    /// <summary>
    /// Appends <paramref name="c"/>.
    /// </summary>
    /// <param name="c">The character to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(char c)
    {
        if (_length == _chars.Length)
            Grow(1);

        _chars[_length++] = c;
    }

    /// <summary>
    /// Appends <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The characters to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(scoped ReadOnlySpan<char> value)
    {
        if (_length + value.Length > _chars.Length)
            Grow(value.Length);

        value.CopyTo(_chars[_length..]);
        _length += value.Length;
    }

    /// <summary>
    /// Appends <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The string to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(string value)
    {
        Append(value.AsSpan());
    }

    /// <summary>
    /// Returns the rented array, if any, to the pool without creating a string, and resets the builder to empty.
    /// </summary>
    public void Dispose()
    {
        char[]? rented = _rented;
        this = default;
        if (rented != null)
            ArrayPool<char>.Shared.Return(rented);
    }

    /// <summary>
    /// Returns the characters written since <paramref name="start"/>.
    /// </summary>
    /// <param name="start">The position to start from.</param>
    /// <returns>A span over the written characters; invalidated by an append that grows the buffer, and by <see cref="ToString"/> or <see cref="Dispose"/>.</returns>
    public readonly ReadOnlySpan<char> Slice(int start)
    {
        return _chars[start.._length];
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Returns the rented array, if any, to the pool and resets the builder to empty, so a second call returns an empty string.
    /// </remarks>
    /// <returns>The built string.</returns>
    public override string ToString()
    {
        string result = _chars[.._length].ToString();
        char[]? rented = _rented;
        this = default;
        if (rented != null)
            ArrayPool<char>.Shared.Return(rented);

        return result;
    }

    /// <summary>
    /// Replaces the buffer with an array rented from the pool of at least twice the capacity or the required length,
    /// whichever is larger, keeping the written characters and returning the previously rented array, if any, to the pool.
    /// </summary>
    /// <param name="additional">The number of characters about to be appended, which the new buffer must have room for.</param>
    private void Grow(int additional)
    {
        char[] next = ArrayPool<char>.Shared.Rent(Math.Max(_chars.Length * 2, _length + additional));
        _chars[.._length].CopyTo(next);
        if (_rented != null)
            ArrayPool<char>.Shared.Return(_rented);

        _rented = next;
        _chars = next;
    }

    /// <summary>
    /// Gets or sets the number of characters written.
    /// </summary>
    /// <remarks>Setting a smaller value truncates the content; the value is not validated and must not exceed the current length.</remarks>
    public int Length
    {
        readonly get => _length;
        set => _length = value;
    }
}