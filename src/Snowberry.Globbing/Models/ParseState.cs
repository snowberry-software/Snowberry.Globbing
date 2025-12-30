using System.Collections.Generic;
using Snowberry.Globbing.Tokens;

namespace Snowberry.Globbing.Models;

/// <summary>
/// Represents the parsing state during glob pattern compilation.
/// </summary>
/// <remarks>
/// This class tracks the state of the parser as it processes a glob pattern, including position,
/// output accumulation, token tracking, and delimiter balancing.
/// </remarks>
public class ParseState
{
    /// <summary>
    /// Gets or sets the original input glob pattern being parsed.
    /// </summary>
    /// <value>A <see cref="string"/> containing the glob pattern.</value>
    public string Input { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current parsing position in the input string.
    /// </summary>
    /// <value>An <see cref="int"/> representing the current index. Default is -1 (not started).</value>
    public int Index { get; set; } = -1;

    /// <summary>
    /// Gets or sets the starting position for parsing.
    /// </summary>
    /// <value>An <see cref="int"/> representing the start index.</value>
    public int Start { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether dotfile matching is enabled for this pattern.
    /// </summary>
    /// <value><see langword="true"/> if dotfiles can be matched; otherwise, <see langword="false"/>.</value>
    public bool Dot { get; set; }

    /// <summary>
    /// Gets or sets the characters consumed from the input during parsing.
    /// </summary>
    /// <value>A <see cref="string"/> containing consumed characters. Default is empty string.</value>
    public string Consumed { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the generated regex pattern output.
    /// </summary>
    /// <value>A <see cref="string"/> containing the regex pattern being built. Default is empty string.</value>
    public string Output { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the prefix portion of the pattern (before any glob characters).
    /// </summary>
    /// <value>A <see cref="string"/> containing the literal prefix. Default is empty string.</value>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether backtracking occurred during parsing.
    /// </summary>
    /// <value><see langword="true"/> if backtracking was required; otherwise, <see langword="false"/>.</value>
    public bool Backtrack { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the pattern is negated (starts with <c>!</c>).
    /// </summary>
    /// <value><see langword="true"/> if the pattern is negated; otherwise, <see langword="false"/>.</value>
    public bool Negated { get; set; }

    /// <summary>
    /// Gets or sets the count of currently open (unmatched) square brackets.
    /// </summary>
    /// <value>An <see cref="int"/> representing open bracket count.</value>
    public int Brackets { get; set; }

    /// <summary>
    /// Gets or sets the count of currently open (unmatched) curly braces.
    /// </summary>
    /// <value>An <see cref="int"/> representing open brace count.</value>
    public int Braces { get; set; }

    /// <summary>
    /// Gets or sets the count of currently open (unmatched) parentheses.
    /// </summary>
    /// <value>An <see cref="int"/> representing open parenthesis count.</value>
    public int Parens { get; set; }

    /// <summary>
    /// Gets or sets the count of currently open (unmatched) quotes.
    /// </summary>
    /// <value>An <see cref="int"/> representing open quote count.</value>
    public int Quotes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the pattern contains a globstar (<c>**</c>).
    /// </summary>
    /// <value><see langword="true"/> if globstar is present; otherwise, <see langword="false"/>.</value>
    public bool Globstar { get; set; }

    /// <summary>
    /// Gets or sets the list of tokens generated during parsing.
    /// </summary>
    /// <value>A <see cref="List{T}"/> of <see cref="Token"/> instances.</value>
    public List<Token> Tokens { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether a negated extglob pattern was encountered.
    /// </summary>
    /// <value><see langword="true"/> if a negated extglob (e.g., <c>!(pattern)</c>) is present; otherwise, <see langword="false"/>.</value>
    public bool NegatedExtglob { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether fast-path optimizations are enabled for this parse.
    /// </summary>
    /// <value><see langword="true"/> to enable fast paths; otherwise, <see langword="false"/>. Default is <see langword="true"/>.</value>
    public bool Fastpaths { get; set; } = true;
}
