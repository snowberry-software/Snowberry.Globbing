namespace Snowberry.Globbing.Compilation;

/// <summary>
/// A run of literal regex text that <see cref="RegexFreeMatcher"/> has read but not yet decoded.
/// </summary>
/// <param name="Start">The index in the regex source the run starts at.</param>
/// <param name="End">The index in the regex source after the run.</param>
/// <param name="Length">The number of characters the run matches.</param>
/// <param name="First">The first character the run matches, or a placeholder if it is a class; meaningful only when <paramref name="Length"/> is not <c>0</c>.</param>
/// <param name="HasClasses">Whether a position is a character class.</param>
/// <param name="HasSlash">Whether the run matches a separator.</param>
internal readonly record struct RegexFreeLiteralRun(int Start, int End, int Length, char First, bool HasClasses, bool HasSlash);
