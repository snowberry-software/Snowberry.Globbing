namespace Snowberry.Globbing.Compilation;

/// <summary>
/// The result of compiling one glob pattern.
/// </summary>
/// <param name="Source">The portable regex source of the pattern, including any negation.</param>
/// <param name="PositiveSource">
/// For a negated pattern compiled without <see cref="GlobOptions.MatchSubstring"/>, <see cref="System.Text.RegularExpressions.RegexOptions.Multiline"/>
/// or <see cref="System.Text.RegularExpressions.RegexOptions.RightToLeft"/>, the anchored regex source of the body without its negation;
/// the pattern matches exactly the inputs without line terminators that this regex does not match. Otherwise, <see langword="null"/>.
/// </param>
/// <param name="Hint">
/// Literal text every input matching <paramref name="PositiveSource"/> must have when that is set, or every input matching
/// <paramref name="Source"/> otherwise; <see langword="null"/> if there is none or it cannot be checked, which is always the case for a
/// negated pattern without <paramref name="PositiveSource"/>.
/// </param>
internal readonly record struct GlobCompilation(string Source, string? PositiveSource, LiteralHint? Hint);
