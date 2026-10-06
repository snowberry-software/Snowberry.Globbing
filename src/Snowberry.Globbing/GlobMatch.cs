namespace Snowberry.Globbing;

/// <summary>
/// The result of matching an input against a <see cref="Glob"/>.
/// </summary>
/// <param name="Success"><see langword="true"/> if the input matched a pattern and no ignore pattern; otherwise, <see langword="false"/>.</param>
/// <param name="IsIgnored"><see langword="true"/> if the input matched a pattern but was excluded by <see cref="GlobOptions.IgnorePatterns"/>; otherwise, <see langword="false"/>.</param>
/// <param name="Input">The input that was matched.</param>
/// <param name="NormalizedInput">The input after <see cref="GlobOptions.InputNormalizer"/> or separator normalization, before <see cref="GlobOptions.MatchFileNameOnly"/> takes its file name.</param>
/// <param name="Pattern">The first pattern that matched, also when the input was ignored, or <see langword="null"/> if no pattern matched.</param>
public readonly record struct GlobMatch(bool Success, bool IsIgnored, string Input, string NormalizedInput, string? Pattern);