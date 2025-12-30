using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Snowberry.Globbing.Models;
using Snowberry.Globbing.Tokens;
using Snowberry.Globbing.Utilities;
using static Snowberry.Globbing.Constants;
#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#endif

namespace Snowberry.Globbing.Parsing;

/// <summary>
/// Instance-based parser for glob patterns that converts them into regex patterns.
/// </summary>
public partial class GlobParser
{
#if NET7_0_OR_GREATER
    [GeneratedRegex(@"(^[*!]|[/()[\\\]{}\""])")]
    private static partial Regex FastpathDetectorGenerated();
    private static readonly Regex s_FastpathDetector = FastpathDetectorGenerated();

    [GeneratedRegex(@"^\([^?]")]
    private static partial Regex ExtglobPatternGenerated();
    private static readonly Regex s_ExtglobPattern = ExtglobPatternGenerated();

    [GeneratedRegex(@"<([!=]|\w+>)")]
    private static partial Regex ParenPatternGenerated();
    private static readonly Regex s_ParenPattern = ParenPatternGenerated();

    [GeneratedRegex(@"^\)+$")]
    private static partial Regex ClosingParensGenerated();
    private static readonly Regex s_ClosingParens = ClosingParensGenerated();

    [GeneratedRegex(@"^\.[^\\/.]+$")]
    private static partial Regex DotPatternGenerated();
    private static readonly Regex s_DotPattern = DotPatternGenerated();

    [GeneratedRegex(@"\\+")]
    private static partial Regex BackslashSequenceGenerated();
    private static readonly Regex s_BackslashSequence = BackslashSequenceGenerated();
#else
    private static readonly Regex s_FastpathDetector = new(@"(^[*!]|[/()[\\\]{}\""])", RegexOptions.Compiled);
    private static readonly Regex s_ExtglobPattern = new(@"^\([^?]", RegexOptions.Compiled);
    private static readonly Regex s_ParenPattern = new(@"<([!=]|\w+>)", RegexOptions.Compiled);
    private static readonly Regex s_ClosingParens = new(@"^\)+$", RegexOptions.Compiled);
    private static readonly Regex s_DotPattern = new(@"^\.[^\\/.]+$", RegexOptions.Compiled);
    private static readonly Regex s_BackslashSequence = new(@"\\+", RegexOptions.Compiled);
#endif

    private static readonly ObjectPool<Token> s_TokenPool = new(() => new Token(), t => t.Reset(), 256);

    private const string EscapedBackslash = "\\\\";
    private const string EscapedDollar = "\\$";
    private const string EscapedCaret = "\\^";
    private const string EscapedOpenBracket = "\\[";
    private const string EscapedCloseBracket = "\\]";
    private const string EscapedOpenBrace = "\\{";
    private const string EscapedCloseBrace = "\\}";
    private const string EscapedOpenParen = "\\(";
    private const string EscapedCloseParen = "\\)";
    private const string EscapedQuestion = "\\?";
    private const string EscapedExclamation = "\\!";

    private readonly GlobbingOptions _options;
    private readonly GlobChars _platformChars;

#if NET8_0_OR_GREATER
    private readonly FrozenDictionary<char, ExtglobChar> _extglobChars;
#else
    private readonly Dictionary<char, ExtglobChar> _extglobChars;
#endif
    private readonly string _capture;
    private readonly string _nodot;
    private readonly string _qmarkNoDot;
    private readonly string _star;
    private readonly string _globstarPattern;

    private ParseState? _state;
    private string _input = "";
    private int _index;
    private int _inputLength;
    private List<Token> _tokens;
    private List<Token> _extglobs;
    private List<Token> _braces;
    private List<DelimiterType> _stack;
    private Token? _prev;
    private Token? _bos;

    private StringBuilder _outputBuilder;
    private StringBuilder _consumedBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobParser"/> class.
    /// </summary>
    /// <param name="options">The parsing options to use.</param>
    public GlobParser(GlobbingOptions? options = null)
    {
        _options = options ?? new GlobbingOptions();

        bool windows = _options.Windows ?? false;
        _platformChars = Constants.GlobChars(windows);
        _extglobChars = ExtglobChars(_platformChars);

        _capture = _options.Capture ? "" : "?:";
        _nodot = _options.Dot ? "" : _platformChars.NO_DOT;
        _qmarkNoDot = _options.Dot ? _platformChars.QMARK : _platformChars.QMARK_NO_DOT;

        string baseStar = _options.Bash ? BuildGlobstar() : _platformChars.STAR;
        _star = _options.Capture ? string.Concat("(", baseStar, ")") : baseStar;
        _globstarPattern = BuildGlobstar();

        _outputBuilder = new StringBuilder(256);
        _consumedBuilder = new StringBuilder(64);

        // Initialize lists once and reuse across parse operations
        _tokens = new List<Token>(32);
        _extglobs = new List<Token>(8);
        _braces = new List<Token>(8);
        _stack = new List<DelimiterType>(16);
    }

    /// <summary>
    /// Parses the input glob pattern and returns the parse state.
    /// </summary>
    /// <param name="input">The glob pattern to parse.</param>
    /// <returns>The resulting parse state containing the generated regex pattern.</returns>
    public ParseState Parse(string input)
    {
        if (string.IsNullOrEmpty(input))
            throw new ArgumentException("Expected a non-empty string", nameof(input));

        if (s_Replacements.TryGetValue(input, out string? replacement))
            input = replacement;

        int max = _options.MaxLength;
        if (input.Length > max)
            throw new ArgumentException($"Input length: {input.Length}, exceeds maximum allowed length: {max}");

        InitializeState(input);

        // Minimatch options support
        if (_options.NoExt.HasValue)
            _options.NoExtglob = _options.NoExt.Value;

        if (TryRemovePrefix(out string? output))
            _input = output!;

        _inputLength = _input.Length;

        // Try fast path first
        if (_options.FastPaths && !s_FastpathDetector.IsMatch(_input))
            return ParseFastPath();

        return ParseFullPath();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryRemovePrefix(out string? output)
    {
        output = null;
        if (_input.Length >= 2 && _input[0] == '.' && _input[1] == '/')
        {
            _state!.Prefix = "./";
            output = _input[2..];
            return true;
        }

        return false;
    }

    private void InitializeState(string input)
    {
        _input = input;
        _inputLength = input.Length;
        _index = -1;

        _bos = RentToken(TokenType.Bos, "", _options.Prepend ?? "");

        _tokens.Clear();
        _tokens.Add(_bos);
        _extglobs.Clear();
        _braces.Clear();
        _stack.Clear();
        _prev = _bos;

        _outputBuilder.Clear();
        _consumedBuilder.Clear();

        _state = new ParseState
        {
            Input = input,
            Index = -1,
            Start = 0,
            Dot = _options.Dot,
            Consumed = "",
            Output = "",
            Prefix = "",
            Backtrack = false,
            Negated = false,
            Brackets = 0,
            Braces = 0,
            Parens = 0,
            Quotes = 0,
            Globstar = false,
            Tokens = _tokens,
            NegatedExtglob = false,
            Fastpaths = _options.FastPaths
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Token RentToken(TokenType type, string value, string? output = null)
    {
        var token = s_TokenPool.Rent();
        token.Type = type;
        token.Value = value;
        token.Output = output;
        return token;
    }

    private ParseState ParseFastPath()
    {
        bool backslashes = false;
        string output = s_RegexSpecialCharsBackref.Replace(_input, (match) =>
        {
            string m = match.Value;
            string esc = match.Groups.Count > 1 ? match.Groups[1].Value : "";
            string chars = match.Groups.Count > 2 ? match.Groups[2].Value : "";

            if (chars.Length == 0)
                return string.IsNullOrEmpty(esc) ? string.Concat("\\", m) : m;

            char first = chars[0];
            int restLength = chars.Length - 1;

            if (first == '\\')
            {
                backslashes = true;
                return m;
            }

            if (first == '?')
                return BuildQmarkOutput(esc, restLength, match.Index);

            if (first == '.')
                return BuildRepeatedString(_platformChars.DOT_LITERAL, chars.Length);

            if (first == '*')
            {
                if (!string.IsNullOrEmpty(esc))
                    return restLength > 0 ? string.Concat(esc, "*", _star) : string.Concat(esc, "*");
                return _star;
            }

            return string.IsNullOrEmpty(esc) ? string.Concat("\\", m) : m;
        });

        if (backslashes)
        {
            output = _options.Unescape == true
                ? output.Replace("\\", "")
                : s_BackslashSequence.Replace(output, m => m.Length % 2 == 0 ? "\\\\" : (m.Length > 0 ? "\\" : ""));
        }

        if (output == _input && _options.Contains)
        {
            _state!.Output = _input;
            return _state;
        }

        _state!.Output = Utils.WrapOutput(output, _state, _options);
        return _state;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string BuildQmarkOutput(string esc, int restLength, int index)
    {
        if (!string.IsNullOrEmpty(esc))
        {
            return restLength > 0
                ? string.Concat(esc, "?", BuildRepeatedString(_platformChars.QMARK, restLength))
                : string.Concat(esc, "?");
        }

        if (index == 0)
        {
            return restLength > 0
                ? string.Concat(_qmarkNoDot, BuildRepeatedString(_platformChars.QMARK, restLength))
                : _qmarkNoDot;
        }

        return BuildRepeatedString(_platformChars.QMARK, restLength + 1);
    }

    private static string BuildRepeatedString(string pattern, int count)
    {
        if (count <= 0)
            return "";
        if (count == 1)
            return pattern;

        var sb = StringBuilderPool.Rent();
        try
        {
            for (int i = 0; i < count; i++)
                sb.Append(pattern);
            return sb.ToString();
        }
        finally
        {
            StringBuilderPool.Return(sb);
        }
    }

    private ParseState ParseFullPath()
    {
        while (_index < _inputLength - 1)
        {
            char ch = Advance();

            if (ch == '\0')
                continue;

            // If we're inside a regex character class, continue until we reach the closing bracket.
            // This must be checked BEFORE the switch statement to prevent characters like '.', '?', '+', etc.
            // from being handled by their special handlers when inside brackets.
            if (_state!.Brackets > 0 && (ch != ']' || _prev!.Value == "[" || _prev.Value == "[^"))
            {
                HandleBracketContent(ch);
                continue;
            }

            switch (ch)
            {
                case '\\':
                    if (_state!.Quotes == 1)
                        HandleQuotedContent(ch);
                    else
                        HandleEscapedCharacter();
                    break;
                case '"':
                    // Double quote always toggles quote state (opening or closing)
                    HandleDoubleQuote();
                    break;
                case '(':
                    if (_state!.Quotes == 1)
                        HandleQuotedContent(ch);
                    else
                        HandleOpenParen();
                    break;
                case ')':
                    if (_state!.Quotes == 1)
                        HandleQuotedContent(ch);
                    else
                        HandleCloseParen();
                    break;
                case '[':
                    if (_state!.Quotes == 1)
                        HandleQuotedContent(ch);
                    else
                        HandleOpenBracket();
                    break;
                case ']':
                    if (_state!.Quotes == 1)
                        HandleQuotedContent(ch);
                    else
                        HandleCloseBracket();
                    break;
                case '{':
                    if (_state!.Quotes == 1)
                        HandleQuotedContent(ch);
                    else if (!_options.NoBrace)
                        HandleOpenBrace();
                    else
                        HandlePlainText(ch);
                    break;
                case '}':
                    if (_state!.Quotes == 1)
                        HandleQuotedContent(ch);
                    else
                        HandleCloseBrace();
                    break;
                case '|':
                    if (_state!.Quotes == 1)
                        HandleQuotedContent(ch);
                    else
                        HandlePipe();
                    break;
                case ',':
                    if (_state!.Quotes == 1)
                        HandleQuotedContent(ch);
                    else
                        HandleComma();
                    break;
                case '/':
                    if (_state!.Quotes == 1)
                        HandleQuotedContent(ch);
                    else
                        HandleSlash();
                    break;
                case '.':
                    if (_state!.Quotes == 1)
                        HandleQuotedContent(ch);
                    else
                        HandleDot();
                    break;
                case '?':
                    if (_state!.Quotes == 1)
                        HandleQuotedContent(ch);
                    else
                        HandleQuestionMark();
                    break;
                case '!':
                    if (_state!.Quotes == 1)
                        HandleQuotedContent(ch);
                    else
                        HandleExclamation();
                    break;
                case '+':
                    if (_state!.Quotes == 1)
                        HandleQuotedContent(ch);
                    else
                        HandlePlus();
                    break;
                case '@':
                    if (_state!.Quotes == 1)
                        HandleQuotedContent(ch);
                    else
                        HandleAt();
                    break;
                case '*':
                    if (_state!.Quotes == 1)
                        HandleQuotedContent(ch);
                    else
                        HandleStar();
                    break;
                default:
                    if (_state!.Brackets > 0)
                        HandleBracketContent(ch);
                    else if (_state.Quotes == 1)
                        HandleQuotedContent(ch);
                    else
                        HandlePlainText(ch);
                    break;
            }
        }

        HandleUnclosedDelimiters();

        if (_options.StrictSlashes != true && (_prev!.Type == TokenType.Star || _prev.Type == TokenType.Bracket))
        {
            string maybeSlashOutput = string.Concat(_platformChars.SLASH_LITERAL, "?");
            var token = RentToken(TokenType.MaybeSlash, "", maybeSlashOutput);
            _outputBuilder.Append(maybeSlashOutput);
            _tokens.Add(token);
        }

        RebuildOutputIfBacktracked();

        _state!.Output = _outputBuilder.ToString();
        _state.Consumed = _consumedBuilder.ToString();
        _state.Index = _index;

        return _state;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private char Peek(int n)
    {
        int targetIndex = _index + n;
        return targetIndex < _inputLength ? _input[targetIndex] : '\0';
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private char Advance()
    {
        _index++;
        _state!.Index = _index;
        return _index < _inputLength ? _input[_index] : '\0';
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<char> RemainingSpan()
    {
        int startIndex = _index + 1;
        return startIndex >= _inputLength ? [] : _input.AsSpan(startIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string Remaining()
    {
        int startIndex = _index + 1;
        return startIndex >= _inputLength ? "" : _input[startIndex..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Consume(string val, int num)
    {
        _consumedBuilder.Append(val);
        _index += num;
        _state!.Index = _index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Consume(char val)
    {
        _consumedBuilder.Append(val);
    }

    private void Append(Token token)
    {
        _outputBuilder.Append(token.Output ?? token.Value);
        _consumedBuilder.Append(token.Value);
    }

    private void Push(Token tok)
    {
        if (_prev!.Type == TokenType.Globstar)
        {
            bool isBrace = _state!.Braces > 0 && (tok.Type == TokenType.Comma || tok.Type == TokenType.Brace);
            bool isExtglob = tok.Extglob || (_extglobs.Count > 0 && (tok.Type == TokenType.Pipe || tok.Type == TokenType.Paren));

            if (tok.Type != TokenType.Slash && tok.Type != TokenType.Paren && !isBrace && !isExtglob)
            {
                int removeLength = _prev.Output?.Length ?? 0;
                if (_outputBuilder.Length >= removeLength)
                    _outputBuilder.Length -= removeLength;

                _prev.Type = TokenType.Star;
                _prev.Value = "*";
                _prev.Output = _star;
                _outputBuilder.Append(_prev.Output);
            }
        }

        if (_extglobs.Count > 0 && tok.Type != TokenType.Paren)
            _extglobs[^1].Inner += tok.Value;

        if (!string.IsNullOrEmpty(tok.Value) || !string.IsNullOrEmpty(tok.Output))
            Append(tok);

        if (_prev != null && _prev.Type == TokenType.Text && tok.Type == TokenType.Text)
        {
            _prev.Output = (_prev.Output ?? _prev.Value) + tok.Value;
            _prev.Value += tok.Value;
            return;
        }

        tok.Prev = _prev;
        _tokens.Add(tok);
        _prev = tok;
    }

    /// <summary>
    /// Checks if a character is one of the special paren characters (!, =, &lt;, :).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSpecialParenChar(char c)
    {
        return c is '!' or '=' or '<' or ':';
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Increment(DelimiterType type)
    {
        switch (type)
        {
            case DelimiterType.Parens: _state!.Parens++; break;
            case DelimiterType.Brackets: _state!.Brackets++; break;
            case DelimiterType.Braces: _state!.Braces++; break;
        }

        _stack.Add(type);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Decrement(DelimiterType type)
    {
        switch (type)
        {
            case DelimiterType.Parens: _state!.Parens--; break;
            case DelimiterType.Brackets: _state!.Brackets--; break;
            case DelimiterType.Braces: _state!.Braces--; break;
        }

        if (_stack.Count > 0)
            _stack.RemoveAt(_stack.Count - 1);
    }

    private void HandleEscapedCharacter()
    {
        char next = Peek(1);

        if (next == '/' && !_options.Bash)
            return;

        if (next is '.' or ';')
            return;

        if (next == '\0')
        {
            Push(RentToken(TokenType.Text, EscapedBackslash));
            return;
        }

        // Count leading backslashes
        var remainingSpan = RemainingSpan();
        int backslashCount = 0;
        for (int i = 0; i < remainingSpan.Length && remainingSpan[i] == '\\'; i++)
            backslashCount++;

        string value = "\\";
        if (backslashCount > 2)
        {
            _index += backslashCount;
            _state!.Index = _index;
            if (backslashCount % 2 != 0)
                value = EscapedBackslash;
        }

        char advancedChar = Advance();
        string finalValue = _options.Unescape == true
            ? CharStrings.Get(advancedChar)
            : string.Concat(value, CharStrings.Get(advancedChar));

        if (_state!.Brackets == 0)
            Push(RentToken(TokenType.Text, finalValue));
    }

    private void HandleBracketContent(char ch)
    {
        string value = CharStrings.Get(ch);

        if (_options.Posix && ch == ':')
        {
            string prevValue = _prev!.Value;
            if (prevValue.Length > 1)
            {
                var inner = prevValue.AsSpan(1);
                if (inner.IndexOf('[') >= 0 && inner.IndexOf(':') >= 0)
                {
                    _prev.Posix = true;
                    // Find the last occurrence of "[:" sequence, not just "["
                    // This is important when expanded POSIX classes contain literal brackets
                    int idx = prevValue.LastIndexOf("[:");
                    if (idx >= 0)
                    {
                        string pre = prevValue[..idx];
                        string restVal = prevValue[(idx + 2)..];

                        if (s_PosixRegexSource.TryGetValue(restVal, out string? posix))
                        {
                            _prev.Value = string.Concat(pre, posix);
                            _state!.Backtrack = true;
                            Advance();

                            if (string.IsNullOrEmpty(_bos!.Output) && _tokens.Count == 2 && _tokens[1] == _prev)
                                _bos.Output = _platformChars.ONE_CHAR;
                            return;
                        }
                    }
                }
            }
        }

        if ((ch == '[' && Peek(1) != ':') || (ch == '-' && Peek(1) == ']'))
        {
            _prev!.Value = string.Concat(_prev.Value, "\\", value);
            _outputBuilder.Append('\\');
            _outputBuilder.Append(value);
            _consumedBuilder.Append(value);
            return;
        }

        if (ch == ']' && (_prev!.Value == "[" || _prev.Value == "[^"))
        {
            _prev.Value = string.Concat(_prev.Value, "\\", value);
            _outputBuilder.Append('\\');
            _outputBuilder.Append(value);
            _consumedBuilder.Append(value);
            return;
        }

        if (_options.Posix && ch == '!' && _prev!.Value == "[")
            value = "^";

        _prev!.Value += value;
        _outputBuilder.Append(value);
        _consumedBuilder.Append(value);
    }

    private void HandleQuotedContent(char ch)
    {
        string value = Utils.EscapeRegex(CharStrings.Get(ch));
        _prev!.Value += value;
        _outputBuilder.Append(value);
        _consumedBuilder.Append(value);
    }

    private void HandleDoubleQuote()
    {
        _state!.Quotes = _state.Quotes == 1 ? 0 : 1;
        if (_options.KeepQuotes)
            Push(RentToken(TokenType.Text, "\""));
    }

    private void HandleOpenParen()
    {
        Increment(DelimiterType.Parens);
        Push(RentToken(TokenType.Paren, "("));
    }

    private void HandleCloseParen()
    {
        if (_state!.Parens == 0 && _options.StrictBrackets == true)
            throw new ArgumentException(SyntaxError("opening", '('));

        var extglob = _extglobs.Count > 0 ? _extglobs[^1] : null;
        if (extglob != null && _state.Parens == extglob.Parens + 1)
        {
            ExtglobClose(extglob);
            return;
        }

        Push(RentToken(TokenType.Paren, ")", _state.Parens > 0 ? ")" : EscapedCloseParen));
        Decrement(DelimiterType.Parens);
    }

    private void HandleOpenBracket()
    {
        string value = "[";
        var remainingSpan = RemainingSpan();

        if (_options.NoBracket == true || remainingSpan.IndexOf(']') < 0)
        {
            if (_options.NoBracket != true && _options.StrictBrackets == true)
                throw new ArgumentException(SyntaxError("closing", ']'));
            value = EscapedOpenBracket;
        }
        else
        {
            Increment(DelimiterType.Brackets);
        }

        Push(RentToken(TokenType.Bracket, value));
    }

    private void HandleCloseBracket()
    {
        if (_options.NoBracket == true || (_prev != null && _prev.Type == TokenType.Bracket && _prev.Value.Length == 1))
        {
            Push(RentToken(TokenType.Text, "]", EscapedCloseBracket));
            return;
        }

        if (_state!.Brackets == 0)
        {
            if (_options.StrictBrackets == true)
                throw new ArgumentException(SyntaxError("opening", '['));
            Push(RentToken(TokenType.Text, "]", EscapedCloseBracket));
            return;
        }

        Decrement(DelimiterType.Brackets);

        string prevValue = _prev!.Value.Length > 1 ? _prev.Value[1..] : "";

        if (!_prev.Posix && prevValue.Length > 0 && prevValue[0] == '^' && prevValue.IndexOf('/') < 0)
        {
            _prev.Value += "/]";
            _outputBuilder.Append("/]");
            _consumedBuilder.Append("/]");
        }
        else
        {
            _prev.Value += "]";
            _outputBuilder.Append(']');
            _consumedBuilder.Append(']');
        }

        if (_options.LiteralBrackets == false || Utils.HasRegexChars(prevValue))
            return;

        string escaped = Utils.EscapeRegex(_prev.Value);
        int prevValueLength = _prev.Value.Length;
        if (_outputBuilder.Length >= prevValueLength)
            _outputBuilder.Length -= prevValueLength;

        if (_options.LiteralBrackets == true)
        {
            _outputBuilder.Append(escaped);
            _prev.Value = escaped;
            return;
        }

        _prev.Value = string.Concat("(", _capture, escaped, "|", _prev.Value, ")");
        _outputBuilder.Append(_prev.Value);
    }

    private void HandleOpenBrace()
    {
        Increment(DelimiterType.Braces);

        var open = RentToken(TokenType.Brace, "{", "(");
        open.OutputIndex = _outputBuilder.Length;
        open.TokensIndex = _state!.Tokens.Count;

        _braces.Add(open);
        Push(open);
    }

    private void HandleCloseBrace()
    {
        var brace = _braces.Count > 0 ? _braces[^1] : null;

        if (_options.NoBrace || brace == null)
        {
            Push(RentToken(TokenType.Text, "}", "}"));
            return;
        }

        string output = ")";

        if (brace.Dots)
        {
            var range = new List<string>();
            int tokensToRemove = 0;

            for (int i = _tokens.Count - 1; i >= 0; i--)
            {
                if (_tokens[i].Type == TokenType.Brace)
                {
                    tokensToRemove++;
                    break;
                }

                if (_tokens[i].Type != TokenType.Dots)
                    range.Add(_tokens[i].Value);
                tokensToRemove++;
            }

            // Reverse since we collected in reverse order
            range.Reverse();

            if (tokensToRemove > 0)
                _tokens.RemoveRange(_tokens.Count - tokensToRemove, tokensToRemove);

            output = ExpandRange([.. range], _options);
            _state!.Backtrack = true;
        }

        if (!brace.Comma && !brace.Dots)
        {
            _outputBuilder.Length = brace.OutputIndex;

            var toks = _state!.Tokens.Skip(brace.TokensIndex);
            brace.Value = brace.Output = EscapedOpenBrace;

            foreach (var t in toks)
                _outputBuilder.Append(t.Output ?? t.Value);

            Push(RentToken(TokenType.Brace, EscapedCloseBrace, EscapedCloseBrace));
        }
        else
        {
            Push(RentToken(TokenType.Brace, "}", output));
        }

        Decrement(DelimiterType.Braces);
        _braces.RemoveAt(_braces.Count - 1);
    }

    private void HandlePipe()
    {
        if (_extglobs.Count > 0)
            _extglobs[^1].Conditions++;
        Push(RentToken(TokenType.Text, "|"));
    }

    private void HandleComma()
    {
        string commaOutput = ",";
        var brace = _braces.Count > 0 ? _braces[^1] : null;

        if (brace != null && _stack.Count > 0 && _stack[^1] == DelimiterType.Braces)
        {
            brace.Comma = true;
            commaOutput = "|";
        }

        Push(RentToken(TokenType.Comma, ",", commaOutput));
    }

    private void HandleSlash()
    {
        if (_prev!.Type == TokenType.Dot && _index == _state!.Start + 1)
        {
            _state.Start = _index + 1;
            _consumedBuilder.Clear();
            _outputBuilder.Clear();
            _tokens.RemoveAt(_tokens.Count - 1);
            _prev = _bos;
            return;
        }

        Push(RentToken(TokenType.Slash, "/", _platformChars.SLASH_LITERAL));
    }

    private void HandleDot()
    {
        if (_state!.Braces > 0 && _prev!.Type == TokenType.Dot)
        {
            if (_prev.Value == ".")
                _prev.Output = _platformChars.DOT_LITERAL;

            var brace = _braces.Count > 0 ? _braces[^1] : null;
            _prev.Type = TokenType.Dots;
            _prev.Output += ".";
            _prev.Value += ".";
            brace?.Dots = true;
            return;
        }

        if ((_state.Braces + _state.Parens) == 0 && _prev!.Type != TokenType.Bos && _prev.Type != TokenType.Slash)
        {
            Push(RentToken(TokenType.Text, ".", _platformChars.DOT_LITERAL));
            return;
        }

        Push(RentToken(TokenType.Dot, ".", _platformChars.DOT_LITERAL));
    }

    private void HandleQuestionMark()
    {
        bool isGroup = _prev != null && _prev.Value == "(";
        if (!isGroup && !_options.NoExtglob && Peek(1) == '(' && Peek(2) != '?')
        {
            ExtglobOpen(ExtglobType.Qmark, "?");
            return;
        }

        if (_prev != null && _prev.Type == TokenType.Paren)
        {
            char next = Peek(1);
            string qOutput = "?";

            if ((_prev.Value == "(" && !IsSpecialParenChar(next)) || (next == '<' && !s_ParenPattern.IsMatch(Remaining())))
                qOutput = EscapedQuestion;

            Push(RentToken(TokenType.Text, "?", qOutput));
            return;
        }

        if (!_options.Dot && (_prev!.Type == TokenType.Slash || _prev.Type == TokenType.Bos))
        {
            Push(RentToken(TokenType.Qmark, "?", _platformChars.QMARK_NO_DOT));
            return;
        }

        Push(RentToken(TokenType.Qmark, "?", _platformChars.QMARK));
    }

    private void HandleExclamation()
    {
        if (!_options.NoExtglob && Peek(1) == '(')
        {
            if (Peek(2) != '?' || !IsSpecialParenChar(Peek(3)))
            {
                ExtglobOpen(ExtglobType.Negate, "!");
                return;
            }
        }

        if (!_options.NoNegate && _index == 0)
        {
            Negate();
            return;
        }

        Push(RentToken(TokenType.Text, "!", EscapedExclamation));
    }

    private void HandlePlus()
    {
        if (!_options.NoExtglob && Peek(1) == '(' && Peek(2) != '?')
        {
            ExtglobOpen(ExtglobType.Plus, "+");
            return;
        }

        // Check if immediately after open paren - use literal plus
        // This handles cases like !(+) where + should be literal, not quantifier
        // Must come BEFORE the type check to match JS behavior
        if (_prev != null && _prev.Value == "(")
        {
            Push(RentToken(TokenType.Plus, "+", _platformChars.PLUS_LITERAL));
            return;
        }

        // Check bracket/paren/brace type - these should use + as regex quantifier
        if ((_prev != null && (_prev.Type == TokenType.Bracket || _prev.Type == TokenType.Paren || _prev.Type == TokenType.Brace)) || _state!.Parens > 0)
        {
            Push(RentToken(TokenType.Plus, "+"));
            return;
        }

        // Fallback: use literal plus (including when opts.regex is false)
        Push(RentToken(TokenType.Plus, _platformChars.PLUS_LITERAL));
    }

    private void HandleAt()
    {
        if (!_options.NoExtglob && Peek(1) == '(' && Peek(2) != '?')
        {
            var token = RentToken(TokenType.At, "@", "");
            token.Extglob = true;
            Push(token);
            return;
        }

        Push(RentToken(TokenType.Text, "@"));
    }

    private void HandlePlainText(char ch)
    {
        string value = CharStrings.Get(ch);

        if (ch == '$')
            value = EscapedDollar;
        else if (ch == '^')
            value = EscapedCaret;

        string remaining = Remaining();
        var match = s_RegexNonSpecialChars.Match(remaining);
        if (match.Success)
        {
            value = string.Concat(value, match.Value);
            _index += match.Value.Length;
            _state!.Index = _index;
        }

        Push(RentToken(TokenType.Text, value));
    }

    private void HandleStar()
    {
        if (_prev != null && (_prev.Type == TokenType.Globstar || _prev.Star))
        {
            _prev.Type = TokenType.Star;
            _prev.Star = true;
            _prev.Value += "*";
            _prev.Output = _star;
            _state!.Backtrack = true;
            _state.Globstar = true;
            Consume('*');
            return;
        }

        string rest = Remaining();
        if (!_options.NoExtglob && s_ExtglobPattern.IsMatch(rest))
        {
            ExtglobOpen(ExtglobType.Star, "*");
            return;
        }

        if (_prev!.Type == TokenType.Star)
        {
            HandleGlobstar(rest);
            return;
        }

        var starToken = RentToken(TokenType.Star, "*", _star);

        if (_options.Bash)
        {
            starToken.Output = ".*?";
            if (_prev.Type is TokenType.Bos or TokenType.Slash)
                starToken.Output = string.Concat(_nodot, starToken.Output);
            Push(starToken);
            return;
        }

        if (_prev != null && (_prev.Type == TokenType.Bracket || _prev.Type == TokenType.Paren) && _options.Regex)
        {
            starToken.Output = "*";
            Push(starToken);
            return;
        }

        if (_index == _state!.Start || _prev!.Type == TokenType.Slash || _prev.Type == TokenType.Dot)
        {
            if (_prev!.Type == TokenType.Dot)
            {
                _outputBuilder.Append(_platformChars.NO_DOT_SLASH);
                _prev.Output += _platformChars.NO_DOT_SLASH;
            }
            else if (_options.Dot)
            {
                _outputBuilder.Append(_platformChars.NO_DOTS_SLASH);
                _prev.Output += _platformChars.NO_DOTS_SLASH;
            }
            else
            {
                _outputBuilder.Append(_nodot);
                _prev.Output += _nodot;
            }

            if (Peek(1) != '*')
            {
                _outputBuilder.Append(_platformChars.ONE_CHAR);
                _prev.Output += _platformChars.ONE_CHAR;
            }
        }

        Push(starToken);
    }

    private void HandleGlobstar(string rest)
    {
        if (_options.NoGlobstar)
        {
            Consume('*');
            return;
        }

        var prior = _prev!.Prev;
        var priorType = prior?.Type;
        var beforeType = prior?.Prev?.Type;
        bool isStart = priorType is TokenType.Slash or TokenType.Bos;
        bool afterStar = beforeType is TokenType.Star or TokenType.Globstar;

        var restSpan = rest.AsSpan();
        if (_options.Bash && (!isStart || (restSpan.Length > 0 && restSpan[0] != '/')))
        {
            Push(RentToken(TokenType.Star, "*", ""));
            return;
        }

        bool isBrace = _state!.Braces > 0 && (priorType == TokenType.Comma || priorType == TokenType.Brace);
        bool isExtglob = _extglobs.Count > 0 && (priorType == TokenType.Pipe || priorType == TokenType.Paren);
        if (!isStart && priorType != TokenType.Paren && !isBrace && !isExtglob)
        {
            Push(RentToken(TokenType.Star, "*", ""));
            return;
        }

        // Strip consecutive `/**/`
        while (restSpan.Length >= 3 && restSpan.StartsWith("/**"))
        {
            char after = _index + 4 < _inputLength ? _input[_index + 4] : '\0';
            if (after is not '\0' and not '/')
                break;

            restSpan = restSpan[3..];
            Consume("/**", 3);
        }

        // Handle case: ** at start of pattern
        if (priorType == TokenType.Bos && _index >= _inputLength - 1)
        {
            _prev.Type = TokenType.Globstar;
            _prev.Value += "*";
            _prev.Output = _globstarPattern;
            _outputBuilder.Clear();
            _outputBuilder.Append(_prev.Output);
            _state.Globstar = true;
            Consume('*');
            return;
        }

        // Handle case: /**$ at end (not at start)
        if (priorType == TokenType.Slash && beforeType != TokenType.Bos && !afterStar && _index >= _inputLength - 1)
        {
            int removeLength = (prior!.Output?.Length ?? 0) + (_prev.Output?.Length ?? 0);
            if (_outputBuilder.Length >= removeLength)
                _outputBuilder.Length -= removeLength;
            prior.Output = string.Concat("(?:", prior.Output);

            _prev.Type = TokenType.Globstar;
            _prev.Output = string.Concat(_globstarPattern, _options.StrictSlashes == true ? ")" : "|$)");
            _prev.Value += "*";
            _state.Globstar = true;
            _outputBuilder.Append(prior.Output);
            _outputBuilder.Append(_prev.Output);
            Consume('*');
            return;
        }

        // Handle case: /**/ in middle (not at start)
        if (priorType == TokenType.Slash && beforeType != TokenType.Bos && restSpan.Length > 0 && restSpan[0] == '/')
        {
            string end = restSpan.Length > 1 ? "|$" : "";

            int removeLength = (prior!.Output?.Length ?? 0) + (_prev.Output?.Length ?? 0);
            if (_outputBuilder.Length >= removeLength)
                _outputBuilder.Length -= removeLength;
            prior.Output = string.Concat("(?:", prior.Output);

            _prev.Type = TokenType.Globstar;
            _prev.Output = string.Concat(_globstarPattern, _platformChars.SLASH_LITERAL, "|", _platformChars.SLASH_LITERAL, end, ")");
            _prev.Value += "*";

            _outputBuilder.Append(prior.Output);
            _outputBuilder.Append(_prev.Output);
            _state.Globstar = true;

            char advChar = Advance();
            _consumedBuilder.Append('*');
            _consumedBuilder.Append(advChar);

            Push(RentToken(TokenType.Slash, "/", ""));
            return;
        }

        // Handle case: **/ at start
        if (priorType == TokenType.Bos && restSpan.Length > 0 && restSpan[0] == '/')
        {
            _prev.Type = TokenType.Globstar;
            _prev.Value += "*";
            _prev.Output = string.Concat("(?:^|", _platformChars.SLASH_LITERAL, "|", _globstarPattern, _platformChars.SLASH_LITERAL, ")");
            _outputBuilder.Clear();
            _outputBuilder.Append(_prev.Output);
            _state.Globstar = true;

            char advChar = Advance();
            _consumedBuilder.Append('*');
            _consumedBuilder.Append(advChar);

            Push(RentToken(TokenType.Slash, "/", ""));
            return;
        }

        // Handle case: {**/*... at start of brace or extglob
        if (isBrace && restSpan.Length > 0 && restSpan[0] == '/')
        {
            _prev.Type = TokenType.Globstar;
            _prev.Value += "*";
            _prev.Output = string.Concat("(?:^|", _platformChars.SLASH_LITERAL, "|", _globstarPattern, _platformChars.SLASH_LITERAL, ")");
            _outputBuilder.Append(_prev.Output);
            _state.Globstar = true;

            char advChar = Advance();
            _consumedBuilder.Append('*');
            _consumedBuilder.Append(advChar);

            Push(RentToken(TokenType.Slash, "/", ""));
            return;
        }

        // Default case
        int prevOutputLength = _prev.Output?.Length ?? 0;
        if (_outputBuilder.Length >= prevOutputLength)
            _outputBuilder.Length -= prevOutputLength;

        _prev.Type = TokenType.Globstar;
        _prev.Output = _globstarPattern;
        _prev.Value += "*";

        _outputBuilder.Append(_prev.Output);
        _state.Globstar = true;
        Consume('*');
    }

    private void ExtglobOpen(ExtglobType type, string value)
    {
        var extglobChar = _extglobChars[value[0]];
        var token = RentToken(extglobChar.Type, value, _outputBuilder.ToString());
        token.Open = extglobChar.Open;
        token.Close = extglobChar.Close;
        token.Conditions = 1;
        token.Inner = "";
        token.Prev = _prev;
        token.Parens = _state!.Parens;

        string output = string.Concat(_options.Capture ? "(" : "", token.Open);

        Increment(DelimiterType.Parens);
        string currentOutput = _outputBuilder.ToString();
        Push(RentToken(extglobChar.Type, value, string.IsNullOrEmpty(currentOutput) ? _platformChars.ONE_CHAR : ""));

        char advChar = Advance();
        var parenToken = RentToken(TokenType.Paren, CharStrings.Get(advChar), output);
        parenToken.Extglob = true;
        Push(parenToken);

        _extglobs.Add(token);
    }

    private void ExtglobClose(Token token)
    {
        string output = string.Concat(token.Close, _options.Capture ? ")" : "");

        if (token.Type == TokenType.Negate)
        {
            string extglobStar = _star;

            // If the negation contains a slash, use globstar pattern to allow matching across directories
            // JS: if (token.inner && token.inner.length > 1 && token.inner.includes('/'))
            if (token.Inner != null && token.Inner.Length > 1 && token.Inner.IndexOf('/') >= 0)
            {
                extglobStar = _globstarPattern;
            }

            string? remaining = null;
            if (extglobStar != _star || Peek(1) == '\0' || s_ClosingParens.IsMatch(remaining = GetRemaining()))
                output = token.Close = string.Concat(")$))", extglobStar);

            if (token.Inner != null && token.Inner.IndexOf('*') >= 0)
            {
                remaining ??= GetRemaining();
                if (s_DotPattern.IsMatch(remaining))
                {
                    // Any non-magical string (`.ts`) or even nested expression (`.{ts,tsx}`) can follow after the closing parenthesis.
                    // In this case, we need to parse the string and use it in the output of the original pattern.
                    // Suitable patterns: `/!(*.d).ts`, `/!(*.d).{ts,tsx}`, `**/!(*-dbg).@(js)`.
                    var subOptions = _options.GetIgnoreOptions();
                    subOptions.FastPaths = false;
                    var subParser = new GlobParser(subOptions);
                    var subParsed = subParser.Parse(remaining);
                    output = token.Close = string.Concat(")", subParsed.Output, ")", extglobStar, ")");
                }
            }

            if (token.Prev != null && token.Prev.Type == TokenType.Bos)
                _state!.NegatedExtglob = true;
        }

        var closeToken = RentToken(TokenType.Paren, ")", output);
        closeToken.Extglob = true;
        Push(closeToken);

        Decrement(DelimiterType.Parens);
        _extglobs.RemoveAt(_extglobs.Count - 1);
    }

    private string GetRemaining()
    {
        int startIdx = _index + 1;
        return startIdx >= _inputLength ? "" : _input[startIdx..];
    }

    private bool Negate()
    {
        int count = 1;
        while (Peek(1) == '!' && (Peek(2) != '(' || Peek(3) == '?'))
        {
            Advance();
            _state!.Start++;
            count++;
        }

        if (count % 2 == 0)
            return false;

        _state!.Negated = true;
        _state.Start++;
        return true;
    }

    private string BuildGlobstar()
    {
        string dotsOrDot = _options.Dot ? _platformChars.DOTS_SLASH : _platformChars.DOT_LITERAL;

        return string.Concat("(", _capture, "(?:(?!", _platformChars.START_ANCHOR_ABSOLUTE, dotsOrDot, ").)*)");

        // NOTE(VNC): Old version.
        //return string.Concat("(", _capture, "(?:(?!", _platformChars.START_ANCHOR, dotsOrDot, ").)*?)");
    }

    private static string SyntaxError(string type, char ch)
    {
        return $"Missing {type}: \"{ch}\" - use \"\\\\{ch}\" to match literal characters";
    }

    internal static string ExpandRange(string[] args, GlobbingOptions options)
    {
        if (options.ExpandRange != null)
            return options.ExpandRange(args, options);

        Array.Sort(args);
        string value = string.Concat("[", string.Join("-", args), "]");

        try
        {
            _ = new Regex(value);
        }
        catch
        {
            return string.Join("..", args.Select(Utils.EscapeRegex));
        }

        return value;
    }

    private void HandleUnclosedDelimiters()
    {
        while (_state!.Brackets > 0)
        {
            if (_options.StrictBrackets == true)
                throw new ArgumentException(SyntaxError("closing", ']'));

            string escaped = Utils.EscapeLast(_outputBuilder.ToString(), '[');
            _outputBuilder.Clear();
            _outputBuilder.Append(escaped);
            Decrement(DelimiterType.Brackets);
        }

        while (_state.Parens > 0)
        {
            if (_options.StrictBrackets == true)
                throw new ArgumentException(SyntaxError("closing", ')'));

            string escaped = Utils.EscapeLast(_outputBuilder.ToString(), '(');
            _outputBuilder.Clear();
            _outputBuilder.Append(escaped);
            Decrement(DelimiterType.Parens);
        }

        while (_state.Braces > 0)
        {
            if (_options.StrictBrackets == true)
                throw new ArgumentException(SyntaxError("closing", '}'));

            string escaped = Utils.EscapeLast(_outputBuilder.ToString(), '{');
            _outputBuilder.Clear();
            _outputBuilder.Append(escaped);
            Decrement(DelimiterType.Braces);
        }
    }

    private void RebuildOutputIfBacktracked()
    {
        if (!_state!.Backtrack)
            return;

        _outputBuilder.Clear();

        for (int i = 0; i < _state.Tokens.Count; i++)
        {
            var token = _state.Tokens[i];
            _outputBuilder.Append(token.Output ?? token.Value);

            if (!string.IsNullOrEmpty(token.Suffix))
                _outputBuilder.Append(token.Suffix);
        }
    }
}
