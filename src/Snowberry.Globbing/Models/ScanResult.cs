using System.Collections.Generic;
using Snowberry.Globbing.Tokens;

namespace Snowberry.Globbing.Models;

/// <summary>
/// Represents the result of scanning a glob pattern for structural information.
/// </summary>
/// <remarks>
/// This class provides detailed structural analysis of a glob pattern, including whether it contains
/// glob constructs, position information, path components, and nesting depth.
/// </remarks>
public class ScanResult
{
    /// <summary>
    /// Gets or sets the literal prefix portion of the pattern (before any glob characters).
    /// </summary>
    /// <value>A <see cref="string"/> containing the prefix. Default is empty string.</value>
    public string Prefix { get; set; } = "";

    /// <summary>
    /// Gets or sets the original input glob pattern that was scanned.
    /// </summary>
    /// <value>A <see cref="string"/> containing the input pattern.</value>
    public string Input { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the starting index where glob characters begin in the pattern.
    /// </summary>
    /// <value>An <see cref="int"/> representing the start position.</value>
    public int Start { get; set; }

    /// <summary>
    /// Gets or sets the base path portion (directory containing the glob pattern).
    /// </summary>
    /// <value>A <see cref="string"/> containing the base path, or <see langword="null"/> if not applicable.</value>
    public string? Base { get; set; }

    /// <summary>
    /// Gets or sets the glob portion of the pattern (excluding literal prefix).
    /// </summary>
    /// <value>A <see cref="string"/> containing just the glob pattern.</value>
    public string Glob { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the pattern contains brace expressions (<c>{}</c>).
    /// </summary>
    /// <value><see langword="true"/> if braces are present; otherwise, <see langword="false"/>.</value>
    public bool IsBrace { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the pattern contains bracket expressions (<c>[]</c>).
    /// </summary>
    /// <value><see langword="true"/> if brackets are present; otherwise, <see langword="false"/>.</value>
    public bool IsBracket { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the pattern contains any glob characters.
    /// </summary>
    /// <value><see langword="true"/> if glob characters are present; otherwise, <see langword="false"/>.</value>
    public bool IsGlob { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the pattern contains extglob expressions (e.g., <c>!()</c>, <c>+()</c>).
    /// </summary>
    /// <value><see langword="true"/> if extglobs are present; otherwise, <see langword="false"/>.</value>
    public bool IsExtglob { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the pattern contains a globstar (<c>**</c>).
    /// </summary>
    /// <value><see langword="true"/> if globstar is present; otherwise, <see langword="false"/>.</value>
    public bool IsGlobstar { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the pattern is negated (starts with <c>!</c>).
    /// </summary>
    /// <value><see langword="true"/> if the pattern is negated; otherwise, <see langword="false"/>.</value>
    public bool Negated { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the pattern contains a negated extglob.
    /// </summary>
    /// <value><see langword="true"/> if negated extglob is present; otherwise, <see langword="false"/>.</value>
    public bool NegatedExtglob { get; set; }

    /// <summary>
    /// Gets or sets the list of tokens extracted from the pattern.
    /// </summary>
    /// <value>A <see cref="List{T}"/> of <see cref="Token"/> instances, or <see langword="null"/> if tokens were not collected.</value>
    public List<Token>? Tokens { get; set; }

    /// <summary>
    /// Gets or sets the positions of slash characters in the pattern.
    /// </summary>
    /// <value>A <see cref="List{T}"/> of <see cref="int"/> indices, or <see langword="null"/> if not collected.</value>
    public List<int>? Slashes { get; set; }

    /// <summary>
    /// Gets or sets the path segments (components between slashes) of the pattern.
    /// </summary>
    /// <value>A <see cref="List{T}"/> of <see cref="string"/> path parts, or <see langword="null"/> if parts were not collected.</value>
    public List<string>? Parts { get; set; }

    /// <summary>
    /// Gets or sets the maximum directory nesting depth implied by the pattern.
    /// </summary>
    /// <value>An <see cref="int"/> representing the maximum depth.</value>
    public int MaxDepth { get; set; }
}
