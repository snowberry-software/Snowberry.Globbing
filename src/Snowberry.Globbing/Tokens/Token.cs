using System;

namespace Snowberry.Globbing.Tokens;

/// <summary>
/// Represents a single token extracted during glob pattern scanning and parsing.
/// </summary>
/// <remarks>
/// Tokens represent meaningful units in a glob pattern, such as literals, wildcards, braces, brackets,
/// extglobs, and other glob constructs. They maintain relationships with adjacent tokens and carry metadata
/// about their type and transformation into regex.
/// </remarks>
public partial class Token
{
    [Flags]
    private enum TokenFlags
    {
        None = 0,
        Extglob = 1 << 0,
        Star = 1 << 1,
        Posix = 1 << 2,
        Backslashes = 1 << 3,
        Comma = 1 << 4,
        Dots = 1 << 5,
        IsBrace = 1 << 6,
        IsBracket = 1 << 7,
        IsGlob = 1 << 8,
        IsExtglob = 1 << 9,
        IsGlobstar = 1 << 10,
        Negated = 1 << 11,
        IsPrefix = 1 << 12
    }

    private TokenFlags _flags;

    /// <summary>
    /// Gets or sets the token type using a strongly-typed enum for better performance and type safety.
    /// </summary>
    /// <value>A <see cref="TokenType"/> enum value describing the token type.</value>
    public TokenType Type { get; set; }

    /// <summary>
    /// Gets or sets the original value from the input pattern.
    /// </summary>
    /// <value>A <see cref="string"/> containing the raw token value.</value>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the regex output generated for this token.
    /// </summary>
    /// <value>A <see cref="string"/> containing the regex representation of this token.</value>
    public string? Output { get; set; }

    /// <summary>
    /// Gets or sets a reference to the previous token in the sequence.
    /// </summary>
    /// <value>A <see cref="Token"/> instance, or <see langword="null"/> if this is the first token.</value>
    public Token? Prev { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this token is part of an extglob pattern.
    /// </summary>
    /// <value><see langword="true"/> if this is an extglob token; otherwise, <see langword="false"/>.</value>
    public bool Extglob
    {
        get => (_flags & TokenFlags.Extglob) != 0;
        set => _flags = value ? _flags | TokenFlags.Extglob : _flags & ~TokenFlags.Extglob;
    }

    /// <summary>
    /// Gets or sets a value indicating whether this token represents a star wildcard (<c>*</c>).
    /// </summary>
    /// <value><see langword="true"/> if this is a star token; otherwise, <see langword="false"/>.</value>
    public bool Star
    {
        get => (_flags & TokenFlags.Star) != 0;
        set => _flags = value ? _flags | TokenFlags.Star : _flags & ~TokenFlags.Star;
    }

    /// <summary>
    /// Gets or sets a value indicating whether POSIX character classes are used in this token.
    /// </summary>
    /// <value><see langword="true"/> if POSIX classes are present; otherwise, <see langword="false"/>.</value>
    public bool Posix
    {
        get => (_flags & TokenFlags.Posix) != 0;
        set => _flags = value ? _flags | TokenFlags.Posix : _flags & ~TokenFlags.Posix;
    }

    /// <summary>
    /// Gets or sets a value indicating whether this token contains backslashes.
    /// </summary>
    /// <value><see langword="true"/> if backslashes are present; otherwise, <see langword="false"/>.</value>
    public bool Backslashes
    {
        get => (_flags & TokenFlags.Backslashes) != 0;
        set => _flags = value ? _flags | TokenFlags.Backslashes : _flags & ~TokenFlags.Backslashes;
    }

    /// <summary>
    /// Gets or sets the inner content of a delimited token (e.g., content inside braces or brackets).
    /// </summary>
    /// <value>A <see cref="string"/> containing the inner content, or <see langword="null"/> if not applicable.</value>
    public string? Inner { get; set; }

    /// <summary>
    /// Gets or sets the count of parentheses associated with this token.
    /// </summary>
    /// <value>An <see cref="int"/> representing parenthesis count.</value>
    public int Parens { get; set; }

    /// <summary>
    /// Gets or sets the count of conditions (alternatives) in a pattern like braces or extglobs.
    /// </summary>
    /// <value>An <see cref="int"/> representing the number of conditions/alternatives.</value>
    public int Conditions { get; set; }

    /// <summary>
    /// Gets or sets the closing delimiter for this token (e.g., <c>}</c>, <c>]</c>, <c>)</c>).
    /// </summary>
    /// <value>A <see cref="string"/> containing the closing delimiter, or <see langword="null"/> if not applicable.</value>
    public string? Close { get; set; }

    /// <summary>
    /// Gets or sets the opening delimiter for this token (e.g., <c>{</c>, <c>[</c>, <c>(</c>).
    /// </summary>
    /// <value>A <see cref="string"/> containing the opening delimiter, or <see langword="null"/> if not applicable.</value>
    public string? Open { get; set; }

    /// <summary>
    /// Gets or sets a suffix string appended to this token's output.
    /// </summary>
    /// <value>A <see cref="string"/> containing the suffix, or <see langword="null"/> if none.</value>
    public string? Suffix { get; set; }

    /// <summary>
    /// Gets or sets the index of this token in the output regex string.
    /// </summary>
    /// <value>An <see cref="int"/> representing the output position.</value>
    public int OutputIndex { get; set; }

    /// <summary>
    /// Gets or sets the index of this token in the tokens list.
    /// </summary>
    /// <value>An <see cref="int"/> representing the token position.</value>
    public int TokensIndex { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this token represents or contains a comma separator.
    /// </summary>
    /// <value><see langword="true"/> if a comma is present; otherwise, <see langword="false"/>.</value>
    public bool Comma
    {
        get => (_flags & TokenFlags.Comma) != 0;
        set => _flags = value ? _flags | TokenFlags.Comma : _flags & ~TokenFlags.Comma;
    }

    /// <summary>
    /// Gets or sets a value indicating whether this token contains dot characters relevant to dotfile matching.
    /// </summary>
    /// <value><see langword="true"/> if dots are significant; otherwise, <see langword="false"/>.</value>
    public bool Dots
    {
        get => (_flags & TokenFlags.Dots) != 0;
        set => _flags = value ? _flags | TokenFlags.Dots : _flags & ~TokenFlags.Dots;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the pattern contains brace expressions (<c>{}</c>).
    /// </summary>
    /// <value><see langword="true"/> if braces are present; otherwise, <see langword="false"/>.</value>
    public bool IsBrace
    {
        get => (_flags & TokenFlags.IsBrace) != 0;
        set => _flags = value ? _flags | TokenFlags.IsBrace : _flags & ~TokenFlags.IsBrace;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the pattern contains bracket expressions (<c>[]</c>).
    /// </summary>
    /// <value><see langword="true"/> if brackets are present; otherwise, <see langword="false"/>.</value>
    public bool IsBracket
    {
        get => (_flags & TokenFlags.IsBracket) != 0;
        set => _flags = value ? _flags | TokenFlags.IsBracket : _flags & ~TokenFlags.IsBracket;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the pattern contains any glob characters.
    /// </summary>
    /// <value><see langword="true"/> if glob characters are present; otherwise, <see langword="false"/>.</value>
    public bool IsGlob
    {
        get => (_flags & TokenFlags.IsGlob) != 0;
        set => _flags = value ? _flags | TokenFlags.IsGlob : _flags & ~TokenFlags.IsGlob;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the pattern contains extglob expressions (e.g., <c>!()</c>, <c>+()</c>).
    /// </summary>
    /// <value><see langword="true"/> if extglobs are present; otherwise, <see langword="false"/>.</value>
    public bool IsExtglob
    {
        get => (_flags & TokenFlags.IsExtglob) != 0;
        set => _flags = value ? _flags | TokenFlags.IsExtglob : _flags & ~TokenFlags.IsExtglob;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the pattern contains a globstar (<c>**</c>).
    /// </summary>
    /// <value><see langword="true"/> if globstar is present; otherwise, <see langword="false"/>.</value>
    public bool IsGlobstar
    {
        get => (_flags & TokenFlags.IsGlobstar) != 0;
        set => _flags = value ? _flags | TokenFlags.IsGlobstar : _flags & ~TokenFlags.IsGlobstar;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the pattern is negated (starts with <c>!</c>).
    /// </summary>
    /// <value><see langword="true"/> if the pattern is negated; otherwise, <see langword="false"/>.</value>
    public bool Negated
    {
        get => (_flags & TokenFlags.Negated) != 0;
        set => _flags = value ? _flags | TokenFlags.Negated : _flags & ~TokenFlags.Negated;
    }

    /// <summary>
    /// Gets or sets a value indicating whether this token is a prefix.
    /// </summary>
    /// <value><see langword="true"/> if this is a prefix token; otherwise, <see langword="false"/>.</value>
    public bool IsPrefix
    {
        get => (_flags & TokenFlags.IsPrefix) != 0;
        set => _flags = value ? _flags | TokenFlags.IsPrefix : _flags & ~TokenFlags.IsPrefix;
    }

    /// <summary>
    /// Resets all token fields to their default values for pooling.
    /// </summary>
    internal void Reset()
    {
        Type = TokenType.Text;
        Value = string.Empty;
        Output = null;
        Prev = null;
        _flags = TokenFlags.None;
        Inner = null;
        Parens = 0;
        Conditions = 0;
        Close = null;
        Open = null;
        Suffix = null;
        OutputIndex = 0;
        TokensIndex = 0;
    }
}
