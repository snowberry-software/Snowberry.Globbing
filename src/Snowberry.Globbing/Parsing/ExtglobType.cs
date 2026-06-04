namespace Snowberry.Globbing.Parsing;

/// <summary>
/// Represents the type of extended glob (extglob) pattern.
/// </summary>
internal enum ExtglobType
{
    /// <summary>
    /// Question mark extglob: <c>?(pattern)</c> - matches zero or one occurrence.
    /// </summary>
    Qmark,

    /// <summary>
    /// Negation extglob: <c>!(pattern)</c> - matches anything except the pattern.
    /// </summary>
    Negate,

    /// <summary>
    /// Plus extglob: <c>+(pattern)</c> - matches one or more occurrences.
    /// </summary>
    Plus,

    /// <summary>
    /// Star extglob: <c>*(pattern)</c> - matches zero or more occurrences.
    /// </summary>
    Star,

    /// <summary>
    /// At extglob: <c>@(pattern)</c> - matches exactly one occurrence.
    /// </summary>
    At
}
