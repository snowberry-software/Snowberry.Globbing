using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Snowberry.Globbing.Utilities;

/// <summary>
/// Argument validation helpers.
/// </summary>
internal static class Guard
{
    /// <summary>
    /// Throws <see cref="ArgumentNullException"/> if <paramref name="value"/> is <see langword="null"/>.
    /// </summary>
    /// <param name="value">The argument to validate.</param>
    /// <param name="paramName">The name of the argument, captured automatically.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static void NotNull([NotNull] object? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(value, paramName);
#else
        if (value is null)
            throw new ArgumentNullException(paramName);
#endif
    }
}