namespace Snowberry.Globbing.Parsing;

/// <summary>
/// Represents the type of extended glob (extglob) pattern.
/// </summary>
internal enum ExtglobType
{
    /// <summary>
    /// Question mark extglob: ?(pattern) - matches zero or one occurrence.
    /// </summary>
    Qmark,

    /// <summary>
    /// Negation extglob: !(pattern) - matches anything except the pattern.
    /// </summary>
    Negate,

    /// <summary>
    /// Plus extglob: +(pattern) - matches one or more occurrences.
    /// </summary>
    Plus,

    /// <summary>
    /// Star extglob: *(pattern) - matches zero or more occurrences.
    /// </summary>
    Star,

    /// <summary>
    /// At extglob: @(pattern) - matches exactly one occurrence.
    /// </summary>
    At
}
