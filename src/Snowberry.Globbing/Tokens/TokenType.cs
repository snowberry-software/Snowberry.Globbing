namespace Snowberry.Globbing.Tokens;

/// <summary>
/// Defines the type of token in a parsed glob pattern.
/// Using an enum provides better performance and type safety compared to string-based types.
/// </summary>
public enum TokenType
{
    /// <summary>
    /// Beginning of string marker.
    /// </summary>
    Bos,

    /// <summary>
    /// End of string marker.
    /// </summary>
    Eos,

    /// <summary>
    /// Plain text/literal characters.
    /// </summary>
    Text,

    /// <summary>
    /// Single slash separator (/).
    /// </summary>
    Slash,

    /// <summary>
    /// Single asterisk wildcard (*).
    /// </summary>
    Star,

    /// <summary>
    /// Double asterisk globstar (**).
    /// </summary>
    Globstar,

    /// <summary>
    /// Question mark single character wildcard (?).
    /// </summary>
    Qmark,

    /// <summary>
    /// Bracket expression ([...]).
    /// </summary>
    Bracket,

    /// <summary>
    /// Brace expansion ({...}).
    /// </summary>
    Brace,

    /// <summary>
    /// Parenthesis for grouping or extglob ((...)).
    /// </summary>
    Paren,

    /// <summary>
    /// Comma separator in braces or extglobs.
    /// </summary>
    Comma,

    /// <summary>
    /// Pipe separator in extglobs (|).
    /// </summary>
    Pipe,

    /// <summary>
    /// Dot/period character (.).
    /// </summary>
    Dot,

    /// <summary>
    /// Negation operator (!).
    /// </summary>
    Not,

    /// <summary>
    /// Plus sign (+).
    /// </summary>
    Plus,

    /// <summary>
    /// At symbol (@).
    /// </summary>
    At,

    /// <summary>
    /// Backslash escape character (\).
    /// </summary>
    Backslash,

    /// <summary>
    /// Dollar sign ($).
    /// </summary>
    Dollar,

    /// <summary>
    /// Quote character.
    /// </summary>
    Quote,

    /// <summary>
    /// Multiple dots (..).
    /// </summary>
    Dots,

    /// <summary>
    /// Optional slash (used for strict slashes option).
    /// </summary>
    MaybeSlash,

    /// <summary>
    /// Negation token (extglob negation).
    /// </summary>
    Negate
}
