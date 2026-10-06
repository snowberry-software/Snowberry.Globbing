namespace Snowberry.Globbing.Syntax;

/// <summary>
/// The kind of a <see cref="GlobToken"/>.
/// </summary>
internal enum GlobTokenKind : byte
{
    /// <summary>
    /// A literal character, stored in <see cref="GlobToken.Value"/>: an escaped or quoted character, or any character
    /// without a structural meaning under the current options.
    /// </summary>
    Literal,

    /// <summary>A run of <c>*</c>; <see cref="GlobToken.Length"/> is the number of stars.</summary>
    Star,

    /// <summary>A <c>?</c> that does not open an extended glob.</summary>
    Question,

    /// <summary>A <c>/</c>, or an escaped <c>\/</c> unless <see cref="GlobOptions.BashCompatibility"/> is set.</summary>
    Slash,

    /// <summary>A <c>.</c> or an escaped <c>\.</c>.</summary>
    Dot,

    /// <summary><c>,</c>.</summary>
    Comma,

    /// <summary><c>|</c>.</summary>
    Pipe,

    /// <summary>A <c>+</c> that does not open an extended glob.</summary>
    Plus,

    /// <summary>A <c>(</c> that is not part of an extended glob opener.</summary>
    OpenParen,

    /// <summary><c>)</c>.</summary>
    CloseParen,

    /// <summary>A <c>{</c>, when <see cref="GlobOptions.BraceExpansion"/> is set.</summary>
    OpenBrace,

    /// <summary>A <c>}</c>, when <see cref="GlobOptions.BraceExpansion"/> is set.</summary>
    CloseBrace,

    /// <summary>A complete bracket expression such as <c>[a-z]</c>, from <c>[</c> to <c>]</c> inclusive.</summary>
    CharClass,

    /// <summary>
    /// An extended glob opener such as <c>!(</c>, covering the operator and the <c>(</c>; <see cref="GlobToken.Value"/> is the
    /// operator character.
    /// </summary>
    ExtglobOpen,
}
