using System;

namespace Snowberry.Globbing;

/// <summary>
/// Represents regex flags that control pattern matching behavior.
/// </summary>
[Flags]
public enum RegexFlags
{
    /// <summary>
    /// No flags specified. Uses default matching behavior.
    /// </summary>
    None = 0,

    /// <summary>
    /// Case-insensitive matching. Maps to <see cref="System.Text.RegularExpressions.RegexOptions.IgnoreCase"/>.
    /// </summary>
    IgnoreCase = 1 << 0,

    /// <summary>
    /// Multiline mode. Changes ^ and $ to match the beginning and end of each line.
    /// Maps to <see cref="System.Text.RegularExpressions.RegexOptions.Multiline"/>.
    /// </summary>
    MultiLine = 1 << 1,

    /// <summary>
    /// Singleline mode. Allows dot (.) to match newline characters.
    /// Maps to <see cref="System.Text.RegularExpressions.RegexOptions.Singleline"/>.
    /// </summary>
    SingleLine = 1 << 2
}
