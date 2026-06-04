using System.Collections.Generic;
using System.Text.RegularExpressions;
using Snowberry.Globbing.Tokens;
#if NET8_0_OR_GREATER
using System.Buffers;
using System.Collections.Frozen;
#endif

namespace Snowberry.Globbing;

/// <summary>
/// Shared constant values and precompiled regular expressions used across the library.
/// </summary>
public static partial class Constants
{
    /// <summary>The maximum supported length, in characters, of a glob pattern.</summary>
    public const int c_MaxLength = 1024 * 64;

    /// <summary>The left parenthesis character <c>(</c>.</summary>
    public const char c_CharLeftParentheses = '(';

    /// <summary>The right parenthesis character <c>)</c>.</summary>
    public const char c_CharRightParentheses = ')';

    /// <summary>The asterisk character <c>*</c>.</summary>
    public const char c_CharAsterisk = '*';

    /// <summary>The at-sign character <c>@</c>.</summary>
    public const char c_CharAt = '@';

    /// <summary>The backslash character <c>\</c>.</summary>
    public const char c_CharBackwardSlash = '\\';

    /// <summary>The comma character <c>,</c>.</summary>
    public const char c_CharComma = ',';

    /// <summary>The dot character <c>.</c>.</summary>
    public const char c_CharDot = '.';

    /// <summary>The exclamation mark character <c>!</c>.</summary>
    public const char c_CharExclamationMark = '!';

    /// <summary>The forward slash character <c>/</c>.</summary>
    public const char c_CharForwardSlash = '/';

    /// <summary>The left curly brace character <c>{</c>.</summary>
    public const char c_CharLeftCurlyBrace = '{';

    /// <summary>The left square bracket character <c>[</c>.</summary>
    public const char c_CharLeftSquareBracket = '[';

    /// <summary>The line feed character <c>\n</c>.</summary>
    public const char c_CharLineFeed = '\n';

    /// <summary>The plus character <c>+</c>.</summary>
    public const char c_CharPlus = '+';

    /// <summary>The question mark character <c>?</c>.</summary>
    public const char c_CharQuestionMark = '?';

    /// <summary>The right angle bracket character <c>&gt;</c>.</summary>
    public const char c_CharRightAngleBracket = '>';

    /// <summary>The right curly brace character <c>}</c>.</summary>
    public const char c_CharRightCurlyBrace = '}';

    /// <summary>The right square bracket character <c>]</c>.</summary>
    public const char c_CharRightSquareBracket = ']';

#if NET7_0_OR_GREATER
    [GeneratedRegex(@"^[^@![\].,$*+?^{}()|\\/]+")]
    private static partial Regex RegexNonSpecialCharsGenerated();

    /// <summary>A compiled <see cref="Regex"/> that matches a leading run of characters that are not glob special characters.</summary>
    public static readonly Regex s_RegexNonSpecialChars = RegexNonSpecialCharsGenerated();

    [GeneratedRegex(@"[-*+?.^${}(|)[\]]")]
    private static partial Regex RegexSpecialCharsGenerated();

    /// <summary>A compiled <see cref="Regex"/> that matches a single glob special character.</summary>
    public static readonly Regex s_RegexSpecialChars = RegexSpecialCharsGenerated();

    [GeneratedRegex(@"(\\?)((\W)(\3*))")]
    private static partial Regex RegexSpecialCharsBackrefGenerated();

    /// <summary>A compiled <see cref="Regex"/> that matches a special character optionally preceded by a backslash, capturing repeated runs.</summary>
    public static readonly Regex s_RegexSpecialCharsBackref = RegexSpecialCharsBackrefGenerated();

    [GeneratedRegex(@"([-*+?.^${}(|)[\]])")]
    private static partial Regex RegexSpecialCharsGlobalGenerated();

    /// <summary>A compiled <see cref="Regex"/> that captures each glob special character for global replacement.</summary>
    public static readonly Regex s_RegexSpecialCharsGlobal = RegexSpecialCharsGlobalGenerated();

    [GeneratedRegex(@"(?:\[.*?[^\\]\]|\\(?=.))")]
    private static partial Regex RegexRemoveBackslashGenerated();

    /// <summary>A compiled <see cref="Regex"/> that matches bracket expressions and escaping backslashes used when removing backslashes.</summary>
    public static readonly Regex s_RegexRemoveBackslash = RegexRemoveBackslashGenerated();
#else
    /// <summary>A compiled <see cref="Regex"/> that matches a leading run of characters that are not glob special characters.</summary>
    public static readonly Regex s_RegexNonSpecialChars = new(@"^[^@![\].,$*+?^{}()|\\/]+", RegexOptions.Compiled);

    /// <summary>A compiled <see cref="Regex"/> that matches a single glob special character.</summary>
    public static readonly Regex s_RegexSpecialChars = new(@"[-*+?.^${}(|)[\]]", RegexOptions.Compiled);

    /// <summary>A compiled <see cref="Regex"/> that matches a special character optionally preceded by a backslash, capturing repeated runs.</summary>
    public static readonly Regex s_RegexSpecialCharsBackref = new(@"(\\?)((\W)(\3*))", RegexOptions.Compiled);

    /// <summary>A compiled <see cref="Regex"/> that captures each glob special character for global replacement.</summary>
    public static readonly Regex s_RegexSpecialCharsGlobal = new(@"([-*+?.^${}(|)[\]])", RegexOptions.Compiled);

    /// <summary>A compiled <see cref="Regex"/> that matches bracket expressions and escaping backslashes used when removing backslashes.</summary>
    public static readonly Regex s_RegexRemoveBackslash = new(@"(?:\[.*?[^\\]\]|\\(?=.))", RegexOptions.Compiled);
#endif

#if NET8_0_OR_GREATER
    /// <summary>Maps redundant glob token sequences (such as <c>***</c>) to their normalized equivalents.</summary>
    public static readonly FrozenDictionary<string, string> s_Replacements = new Dictionary<string, string>
    {
        ["***"] = "*",
        ["**/**"] = "**",
        ["**/**/**"] = "**"
    }.ToFrozenDictionary();
#else
    /// <summary>Maps redundant glob token sequences (such as <c>***</c>) to their normalized equivalents.</summary>
    public static readonly Dictionary<string, string> s_Replacements = new()
    {
        ["***"] = "*",
        ["**/**"] = "**",
        ["**/**/**"] = "**"
    };
#endif

#if NET8_0_OR_GREATER
    /// <summary>Maps POSIX character class names (such as <c>alnum</c>) to their equivalent regular-expression character ranges.</summary>
    public static readonly FrozenDictionary<string, string> s_PosixRegexSource = new Dictionary<string, string>
    {
        ["alnum"] = "a-zA-Z0-9",
        ["alpha"] = "a-zA-Z",
        ["ascii"] = "\\x00-\\x7F",
        ["blank"] = " \\t",
        ["cntrl"] = "\\x00-\\x1F\\x7F",
        ["digit"] = "0-9",
        ["graph"] = "\\x21-\\x7E",
        ["lower"] = "a-z",
        ["print"] = "\\x20-\\x7E ",
        ["punct"] = "\\-!\"#$%&'()\\*+,./:;<=>?@[\\]^_`{|}~",
        ["space"] = " \\t\\r\\n\\v\\f",
        ["upper"] = "A-Z",
        ["word"] = "A-Za-z0-9_",
        ["xdigit"] = "A-Fa-f0-9"
    }.ToFrozenDictionary();
#else
    /// <summary>Maps POSIX character class names (such as <c>alnum</c>) to their equivalent regular-expression character ranges.</summary>
    public static readonly Dictionary<string, string> s_PosixRegexSource = new()
    {
        ["alnum"] = "a-zA-Z0-9",
        ["alpha"] = "a-zA-Z",
        ["ascii"] = "\\x00-\\x7F",
        ["blank"] = " \\t",
        ["cntrl"] = "\\x00-\\x1F\\x7F",
        ["digit"] = "0-9",
        ["graph"] = "\\x21-\\x7E",
        ["lower"] = "a-z",
        ["print"] = "\\x20-\\x7E ",
        ["punct"] = "\\-!\"#$%&'()\\*+,./:;<=>?@[\\]^_`{|}~",
        ["space"] = " \\t\\r\\n\\v\\f",
        ["upper"] = "A-Z",
        ["word"] = "A-Za-z0-9_",
        ["xdigit"] = "A-Fa-f0-9"
    };
#endif

    private const string c_WinSlash = "\\\\/";
    private const string c_WinNoSlash = "[^\\\\/]";

    // Posix glob constants (kept only if referenced 2+ times across the file)
    private const string c_DotLiteral = "\\.";
    private const string c_PlusLiteral = "\\+";
    private const string c_QmarkLiteral = "\\?";

    // Cached GlobChars instances to avoid repeated allocations
    private static readonly GlobChars s_PosixGlobChars;
    private static readonly GlobChars s_WindowsGlobChars;

    // Cached ExtglobChars dictionaries
#if NET8_0_OR_GREATER
    private static readonly FrozenDictionary<char, ExtglobChar> s_PosixExtglobChars;
    private static readonly FrozenDictionary<char, ExtglobChar> s_WindowsExtglobChars;
#else
    private static readonly Dictionary<char, ExtglobChar> s_PosixExtglobChars;
    private static readonly Dictionary<char, ExtglobChar> s_WindowsExtglobChars;
#endif

    static Constants()
    {
        // Initialize POSIX glob chars
        s_PosixGlobChars = new GlobChars
        {
            DOT_LITERAL = c_DotLiteral,
            PLUS_LITERAL = c_PlusLiteral,
            QMARK_LITERAL = c_QmarkLiteral,
            SLASH_LITERAL = "\\/",
            ONE_CHAR = "(?=.)",
            QMARK = "[^/]",
            END_ANCHOR = "(?:\\/|$)",
            DOTS_SLASH = "\\.{1,2}(?:\\/|$)",
            NO_DOT = "(?!\\.)",
            NO_DOTS = "(?!(?:^|\\/)\\.{1,2}(?:\\/|$))",
            NO_DOT_SLASH = "(?!\\.{0,1}(?:\\/|$))",
            NO_DOTS_SLASH = "(?!\\.{1,2}(?:\\/|$))",
            QMARK_NO_DOT = "[^./]",
            STAR = "[^/]*?",
            START_ANCHOR = "(?:^|\\/)",
            START_ANCHOR_ABSOLUTE = "(?:\\A|\\/)",
            SEP = "/"
        };

        // Initialize Windows glob chars
        s_WindowsGlobChars = new GlobChars
        {
            DOT_LITERAL = c_DotLiteral,
            PLUS_LITERAL = c_PlusLiteral,
            QMARK_LITERAL = c_QmarkLiteral,
            SLASH_LITERAL = $"[{c_WinSlash}]",
            ONE_CHAR = "(?=.)",
            QMARK = c_WinNoSlash,
            END_ANCHOR = $"(?:[{c_WinSlash}]|$)",
            DOTS_SLASH = $"{c_DotLiteral}{{1,2}}(?:[{c_WinSlash}]|$)",
            NO_DOT = "(?!\\.)",
            NO_DOTS = $"(?!(?:^|[{c_WinSlash}]){c_DotLiteral}{{1,2}}(?:[{c_WinSlash}]|$))",
            NO_DOT_SLASH = $"(?!{c_DotLiteral}{{0,1}}(?:[{c_WinSlash}]|$))",
            NO_DOTS_SLASH = $"(?!{c_DotLiteral}{{1,2}}(?:[{c_WinSlash}]|$))",
            QMARK_NO_DOT = $"[^.{c_WinSlash}]",
            STAR = $"{c_WinNoSlash}*?",
            START_ANCHOR = $"(?:^|[{c_WinSlash}])",
            START_ANCHOR_ABSOLUTE = $"(?:\\A|[{c_WinSlash}])",
            SEP = "\\"
        };

        // Initialize extglob chars for both platforms
#if NET8_0_OR_GREATER
        s_PosixExtglobChars = CreateExtglobChars(s_PosixGlobChars).ToFrozenDictionary();
        s_WindowsExtglobChars = CreateExtglobChars(s_WindowsGlobChars).ToFrozenDictionary();
#else
        s_PosixExtglobChars = CreateExtglobChars(s_PosixGlobChars);
        s_WindowsExtglobChars = CreateExtglobChars(s_WindowsGlobChars);
#endif
    }

    /// <summary>
    /// Gets the glob characters for the specified platform.
    /// </summary>
    /// <param name="windows">If <see langword="true"/>, returns Windows-specific glob characters; otherwise, returns POSIX glob characters.</param>
    /// <returns>A <see cref="GlobChars"/> instance with platform-specific regex patterns.</returns>
    public static GlobChars GlobChars(bool windows)
    {
        return windows ? s_WindowsGlobChars : s_PosixGlobChars;
    }

    /// <summary>
    /// Gets the extglob characters associated with the specified glob characters.
    /// </summary>
    /// <param name="chars">The glob characters that determine which extglob set is returned.</param>
    /// <returns>A dictionary mapping each extglob character to its <see cref="ExtglobChar"/> definition.</returns>
#if NET8_0_OR_GREATER
    public static FrozenDictionary<char, ExtglobChar> ExtglobChars(GlobChars chars)
#else
    public static Dictionary<char, ExtglobChar> ExtglobChars(GlobChars chars)
#endif
    {
        return ReferenceEquals(chars, s_WindowsGlobChars) ? s_WindowsExtglobChars : s_PosixExtglobChars;
    }

    private static Dictionary<char, ExtglobChar> CreateExtglobChars(GlobChars chars)
    {
        return new Dictionary<char, ExtglobChar>
        {
            ['!'] = new ExtglobChar { Type = TokenType.Negate, Open = "(?:(?!(?:", Close = $")){chars.STAR})" },
            ['?'] = new ExtglobChar { Type = TokenType.Qmark, Open = "(?:", Close = ")?" },
            ['+'] = new ExtglobChar { Type = TokenType.Plus, Open = "(?:", Close = ")+" },
            ['*'] = new ExtglobChar { Type = TokenType.Star, Open = "(?:", Close = ")*" },
            ['@'] = new ExtglobChar { Type = TokenType.At, Open = "(?:", Close = ")" }
        };
    }
}

/// <summary>
/// Platform-specific regular-expression fragments used to build glob patterns.
/// </summary>
public class GlobChars
{
    /// <summary>The regex source matching a literal dot character.</summary>
    public required string DOT_LITERAL { get; set; }

    /// <summary>The regex source matching a literal plus character.</summary>
    public required string PLUS_LITERAL { get; set; }

    /// <summary>The regex source matching a literal question mark character.</summary>
    public required string QMARK_LITERAL { get; set; }

    /// <summary>The regex source matching a literal path separator.</summary>
    public required string SLASH_LITERAL { get; set; }

    /// <summary>The regex source asserting that at least one character is present.</summary>
    public required string ONE_CHAR { get; set; }

    /// <summary>The regex source matching a single non-separator character.</summary>
    public required string QMARK { get; set; }

    /// <summary>The regex source anchoring the end of a path segment or input.</summary>
    public required string END_ANCHOR { get; set; }

    /// <summary>The regex source matching one or two dots followed by a separator or the end of input.</summary>
    public required string DOTS_SLASH { get; set; }

    /// <summary>The regex source asserting that the current position is not a dot.</summary>
    public required string NO_DOT { get; set; }

    /// <summary>The regex source asserting that the current segment is not a single or double dot segment.</summary>
    public required string NO_DOTS { get; set; }

    /// <summary>The regex source asserting that the current position is not a dot followed by a separator or the end of input.</summary>
    public required string NO_DOT_SLASH { get; set; }

    /// <summary>The regex source asserting that the current position is not one or two dots followed by a separator or the end of input.</summary>
    public required string NO_DOTS_SLASH { get; set; }

    /// <summary>The regex source matching a single character that is neither a dot nor a separator.</summary>
    public required string QMARK_NO_DOT { get; set; }

    /// <summary>The regex source matching zero or more non-separator characters (a single star).</summary>
    public required string STAR { get; set; }

    /// <summary>The regex source anchoring the start of a path segment or input.</summary>
    public required string START_ANCHOR { get; set; }

    /// <summary>The platform path separator character.</summary>
    public required string SEP { get; set; }

    /// <summary>
    /// The regex source anchoring the start of the entire input for start-of-segment checks
    /// evaluated inside lookarounds or mid-pattern. Uses <c>\A</c> rather than <c>^</c>.
    /// </summary>
    public required string START_ANCHOR_ABSOLUTE { get; set; }
}

/// <summary>
/// Defines the regular-expression fragments for a single extended-glob (extglob) operator.
/// </summary>
public class ExtglobChar
{
    /// <summary>The <see cref="TokenType"/> represented by this extglob operator.</summary>
    public TokenType Type { get; set; }

    /// <summary>The regex source emitted at the opening of this extglob group.</summary>
    public required string Open { get; set; }

    /// <summary>The regex source emitted at the closing of this extglob group.</summary>
    public required string Close { get; set; }
}
