using System.Text.RegularExpressions;

namespace Snowberry.Globbing.Models;

/// <summary>
/// Represents the result of a pattern matching operation.
/// </summary>
/// <remarks>
/// This class encapsulates all information about a match attempt, including the input pattern,
/// the compiled regex, the input string, and the match result.
/// </remarks>
public class MatchResult
{
    /// <summary>
    /// Gets or sets the original glob pattern used for matching.
    /// </summary>
    /// <value>A <see cref="string"/> containing the glob pattern.</value>
    public required string Glob { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the input string was transformed to use POSIX-style slashes before matching.
    /// </summary>
    public required bool TransformToPosixSlashes { get; set; }

    /// <summary>
    /// Gets or sets the compiled regular expression derived from the glob pattern.
    /// </summary>
    /// <value>A <see cref="System.Text.RegularExpressions.Regex"/> instance.</value>
    public required Regex Regex { get; init; }

    /// <summary>
    /// Gets or sets the input string that was tested against the pattern.
    /// </summary>
    /// <value>A <see cref="string"/> containing the input being matched.</value>
    public required string Input { get; init; }

    /// <summary>
    /// Gets or sets the formatted output string after processing.
    /// </summary>
    /// <value>A <see cref="string"/> containing the formatted output, or <see langword="null"/> if no formatting was applied.</value>
    public required string? Output { get; init; }

    /// <summary>
    /// Gets or sets the regex match object containing capture groups and match details.
    /// </summary>
    /// <value>A <see cref="System.Text.RegularExpressions.Match"/> instance, or <see langword="null"/> if capturing is disabled.</value>
    public required Match? Match { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the input matched the pattern.
    /// </summary>
    /// <value><see langword="true"/> if the input matches the pattern; otherwise, <see langword="false"/>.</value>
    public required bool IsMatch { get; init; }

    /// <summary>
    /// Gets or sets the options used during the globbing operation.
    /// </summary>
    public required GlobbingOptions Options { get; init; }
}
