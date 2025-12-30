using System.Collections.Generic;
using System.Text.RegularExpressions;
using Snowberry.Globbing.Tokens;
#if NET8_0_OR_GREATER
using System.Buffers;
using System.Collections.Frozen;
#endif

namespace Snowberry.Globbing;

/// <summary>
/// Constants used throughout the library
/// </summary>
public static partial class Constants
{
    public const int c_MaxLength = 1024 * 64;

    public const char c_CharLeftParentheses = '(';
    public const char c_CharRightParentheses = ')';
    public const char c_CharAsterisk = '*';
    public const char c_CharAt = '@';
    public const char c_CharBackwardSlash = '\\';
    public const char c_CharComma = ',';
    public const char c_CharDot = '.';
    public const char c_CharExclamationMark = '!';
    public const char c_CharForwardSlash = '/';
    public const char c_CharLeftCurlyBrace = '{';
    public const char c_CharLeftSquareBracket = '[';
    public const char c_CharLineFeed = '\n';
    public const char c_CharPlus = '+';
    public const char c_CharQuestionMark = '?';
    public const char c_CharRightAngleBracket = '>';
    public const char c_CharRightCurlyBrace = '}';
    public const char c_CharRightSquareBracket = ']';

#if NET7_0_OR_GREATER
    [GeneratedRegex(@"^[^@![\].,$*+?^{}()|\\/]+")]
    private static partial Regex RegexNonSpecialCharsGenerated();
    public static readonly Regex s_RegexNonSpecialChars = RegexNonSpecialCharsGenerated();

    [GeneratedRegex(@"[-*+?.^${}(|)[\]]")]
    private static partial Regex RegexSpecialCharsGenerated();
    public static readonly Regex s_RegexSpecialChars = RegexSpecialCharsGenerated();

    [GeneratedRegex(@"(\\?)((\W)(\3*))")]
    private static partial Regex RegexSpecialCharsBackrefGenerated();
    public static readonly Regex s_RegexSpecialCharsBackref = RegexSpecialCharsBackrefGenerated();

    [GeneratedRegex(@"([-*+?.^${}(|)[\]])")]
    private static partial Regex RegexSpecialCharsGlobalGenerated();
    public static readonly Regex s_RegexSpecialCharsGlobal = RegexSpecialCharsGlobalGenerated();

    [GeneratedRegex(@"(?:\[.*?[^\\]\]|\\(?=.))")]
    private static partial Regex RegexRemoveBackslashGenerated();
    public static readonly Regex s_RegexRemoveBackslash = RegexRemoveBackslashGenerated();
#else
    public static readonly Regex s_RegexNonSpecialChars = new(@"^[^@![\].,$*+?^{}()|\\/]+", RegexOptions.Compiled);
    public static readonly Regex s_RegexSpecialChars = new(@"[-*+?.^${}(|)[\]]", RegexOptions.Compiled);
    public static readonly Regex s_RegexSpecialCharsBackref = new(@"(\\?)((\W)(\3*))", RegexOptions.Compiled);
    public static readonly Regex s_RegexSpecialCharsGlobal = new(@"([-*+?.^${}(|)[\]])", RegexOptions.Compiled);
    public static readonly Regex s_RegexRemoveBackslash = new(@"(?:\[.*?[^\\]\]|\\(?=.))", RegexOptions.Compiled);
#endif

    // Replacements
#if NET8_0_OR_GREATER
    public static readonly FrozenDictionary<string, string> s_Replacements = new Dictionary<string, string>
    {
        ["***"] = "*",
        ["**/**"] = "**",
        ["**/**/**"] = "**"
    }.ToFrozenDictionary();
#else
    public static readonly Dictionary<string, string> s_Replacements = new()
    {
        ["***"] = "*",
        ["**/**"] = "**",
        ["**/**/**"] = "**"
    };
#endif

    // POSIX character classes
#if NET8_0_OR_GREATER
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
    /// Get glob characters based on platform (cached for performance).
    /// </summary>
    /// <param name="windows">If <see langword="true"/>, returns Windows-specific glob characters; otherwise, returns POSIX glob characters.</param>
    /// <returns>A cached <see cref="GlobChars"/> instance with platform-specific regex patterns.</returns>
    public static GlobChars GlobChars(bool windows)
    {
        return windows ? s_WindowsGlobChars : s_PosixGlobChars;
    }

    /// <summary>
    /// Get extglob characters (cached for performance).
    /// </summary>
    /// <param name="chars">The glob characters instance to determine which cached extglob set to return.</param>
    /// <returns>A cached dictionary mapping extglob characters to their <see cref="ExtglobChar"/> definitions.</returns>
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
/// Platform-specific glob characters
/// </summary>
public class GlobChars
{
    public required string DOT_LITERAL { get; set; }
    public required string PLUS_LITERAL { get; set; }
    public required string QMARK_LITERAL { get; set; }
    public required string SLASH_LITERAL { get; set; }
    public required string ONE_CHAR { get; set; }
    public required string QMARK { get; set; }
    public required string END_ANCHOR { get; set; }
    public required string DOTS_SLASH { get; set; }
    public required string NO_DOT { get; set; }
    public required string NO_DOTS { get; set; }
    public required string NO_DOT_SLASH { get; set; }
    public required string NO_DOTS_SLASH { get; set; }
    public required string QMARK_NO_DOT { get; set; }
    public required string STAR { get; set; }
    public required string START_ANCHOR { get; set; }
    public required string SEP { get; set; }

    /// <summary>
    /// Anchor used for "start-of-segment" checks when evaluated inside lookarounds or mid-pattern.
    /// This MUST mean start of the entire input, so it uses <c>\A</c> instead of <c>^</c>.
    /// </summary>
    public required string START_ANCHOR_ABSOLUTE { get; set; }
}

/// <summary>
/// Extglob character definition
/// </summary>
public class ExtglobChar
{
    public TokenType Type { get; set; }
    public required string Open { get; set; }
    public required string Close { get; set; }
}
