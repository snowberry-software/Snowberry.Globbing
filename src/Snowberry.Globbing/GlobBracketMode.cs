namespace Snowberry.Globbing;

/// <summary>
/// Specifies how a bracket expression without regex metacharacters, such as <c>[abc]</c>, is compiled.
/// </summary>
public enum GlobBracketMode
{
    /// <summary>
    /// The expression matches either one character of the class or the bracket text itself, so <c>[abc]</c> matches
    /// <c>a</c> and <c>[abc]</c>.
    /// </summary>
    Auto,

    /// <summary>
    /// The expression matches only the bracket text itself. A leading <c>!</c> is then literal, not a negation.
    /// </summary>
    Literal,

    /// <summary>
    /// The expression matches only one character of the class.
    /// </summary>
    CharacterClass,
}