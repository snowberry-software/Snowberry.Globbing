using System;

namespace Snowberry.Globbing;

/// <summary>
/// The exception that is thrown when a glob pattern cannot be compiled.
/// </summary>
public sealed class GlobParseException : ArgumentException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GlobParseException"/> class.
    /// </summary>
    /// <param name="pattern">The pattern that could not be compiled.</param>
    /// <param name="error">The reason the pattern could not be compiled.</param>
    /// <param name="offset">The zero-based position in <paramref name="pattern"/> where the error was detected, or <c>-1</c> if unknown.</param>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this exception, or <see langword="null"/>.</param>
    public GlobParseException(string pattern, GlobParseError error, int offset, string message, Exception? innerException = null)
        : this(pattern, error, offset, message, nameof(pattern), innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobParseException"/> class for the argument named <paramref name="paramName"/>.
    /// </summary>
    /// <param name="pattern">The pattern that could not be compiled.</param>
    /// <param name="error">The reason the pattern could not be compiled.</param>
    /// <param name="offset">The zero-based position in <paramref name="pattern"/> where the error was detected, or <c>-1</c> if unknown.</param>
    /// <param name="message">The message that describes the error, without the parameter name.</param>
    /// <param name="paramName">The name of the argument that supplied <paramref name="pattern"/>.</param>
    /// <param name="innerException">The exception that caused this exception, or <see langword="null"/>.</param>
    private GlobParseException(string pattern, GlobParseError error, int offset, string message, string paramName, Exception? innerException)
        : base(message, paramName, innerException)
    {
        Pattern = pattern;
        Error = error;
        Offset = offset;
        Description = message;
    }

    /// <summary>
    /// Gets the pattern that could not be compiled.
    /// </summary>
    public string Pattern { get; }

    /// <summary>
    /// Gets the reason the pattern could not be compiled.
    /// </summary>
    public GlobParseError Error { get; }

    /// <summary>
    /// Gets the zero-based position in <see cref="Pattern"/> where the error was detected, or <c>-1</c> if unknown.
    /// </summary>
    /// <remarks>For <see cref="GlobParseError.PatternTooLong"/>, this is <see cref="GlobOptions.MaxPatternLength"/>.</remarks>
    public int Offset { get; }

    /// <summary>
    /// Gets the message that describes the error, without the parameter name that <see cref="ArgumentException.Message"/> appends.
    /// </summary>
    private string Description { get; }

    /// <summary>
    /// Returns this exception reported for the argument named <paramref name="paramName"/>.
    /// </summary>
    /// <param name="paramName">The name of the argument that supplied <see cref="Pattern"/>.</param>
    /// <returns>This instance if <see cref="ArgumentException.ParamName"/> is already <paramref name="paramName"/>; otherwise, a copy with that parameter name.</returns>
    internal GlobParseException ForParameter(string paramName)
    {
        return ParamName == paramName ? this : new GlobParseException(Pattern, Error, Offset, Description, paramName, InnerException);
    }

    /// <summary>
    /// Creates the exception for a <see langword="null"/> or empty pattern.
    /// </summary>
    /// <param name="paramName">The name of the argument that supplied the pattern.</param>
    /// <returns>The exception, with <see cref="GlobParseError.EmptyPattern"/> and an empty <see cref="Pattern"/>.</returns>
    internal static GlobParseException EmptyPattern(string paramName)
    {
        return new GlobParseException(string.Empty, GlobParseError.EmptyPattern, -1, "The pattern must not be null or empty.", paramName, null);
    }

    /// <summary>
    /// Creates the exception for an unbalanced delimiter.
    /// </summary>
    /// <param name="pattern">The pattern.</param>
    /// <param name="error">The reason, which names the missing delimiter.</param>
    /// <param name="offset">The position of the unbalanced delimiter in <paramref name="pattern"/>.</param>
    /// <param name="type">Which side of the pair is missing: <c>opening</c> or <c>closing</c>.</param>
    /// <param name="ch">The missing delimiter character.</param>
    /// <returns>The exception.</returns>
    internal static GlobParseException MissingDelimiter(string pattern, GlobParseError error, int offset, string type, char ch)
    {
        return new GlobParseException(pattern, error, offset, $"Missing {type} \"{ch}\" in pattern \"{pattern}\"; escape it as \"\\{ch}\" to match it literally.");
    }
}