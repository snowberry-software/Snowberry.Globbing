using System;
using System.Runtime.CompilerServices;
using Snowberry.Globbing.Models;
using Snowberry.Globbing.Tokens;
using Snowberry.Globbing.Utilities;
using static Snowberry.Globbing.Constants;

namespace Snowberry.Globbing.Parsing;

/// <summary>
/// Instance-based scanner for quickly analyzing glob patterns and extracting structural information.
/// Optimized for performance using spans, pooling, and minimal allocations.
/// </summary>
public class GlobScanner
{
    private static readonly ObjectPool<PooledList<int>> s_IntListPool = new(
        () => [],
        list => list.Reset(),
        maxSize: 50);

    private static readonly ObjectPool<PooledList<Token>> s_TokenListPool = new(
        () => [],
        list => list.Reset(),
        maxSize: 50);

    private static readonly ObjectPool<PooledList<string>> s_StringListPool = new(
        () => [],
        list => list.Reset(),
        maxSize: 50);

    private readonly ScanOptions _options;
    private readonly ReadOnlyMemory<char> _input;
    private readonly int _length;
    private readonly bool _scanToEnd;

    private int _index;
    private int _start;
    private int _lastIndex;
    private bool _isBrace;
    private bool _isBracket;
    private bool _isGlob;
    private bool _isExtglob;
    private bool _isGlobstar;
    private bool _braceEscaped;
    private bool _backslashes;
    private bool _negated;
    private bool _negatedExtglob;
    private bool _finished;
    private int _braces;
    private char _prev;
    private char _code;

    private PooledList<int>? _slashes;
    private PooledList<Token>? _tokens;
    private PooledList<string>? _parts;
    private Token _token;
    private bool _poolsRented;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobScanner"/> class.
    /// </summary>
    /// <param name="input">The glob pattern to scan.</param>
    /// <param name="options">Optional scanning options.</param>
    public GlobScanner(string input, ScanOptions? options = null)
    {
        _options = options ?? new ScanOptions();
        _input = input.AsMemory();
        _length = input.Length - 1;
        _scanToEnd = _options.Parts || _options.ScanToEnd;

        _index = -1;
        _start = 0;
        _lastIndex = 0;
        _token = new Token { Value = "", IsGlob = false };
        _poolsRented = false;
    }

    /// <summary>
    /// Scans the glob pattern and returns structural information.
    /// </summary>
    /// <returns>A <see cref="ScanResult"/> containing information about the pattern structure.</returns>
    public ScanResult Scan()
    {
        try
        {
            RentPools();
            return ScanInternal();
        }
        finally
        {
            ReturnPools();
        }
    }

    private void RentPools()
    {
        if (_poolsRented)
            return;

        _slashes = s_IntListPool.Rent();
        _tokens = s_TokenListPool.Rent();
        _parts = s_StringListPool.Rent();
        _poolsRented = true;
    }

    private void ReturnPools()
    {
        if (!_poolsRented)
            return;

        if (_slashes != null)
        {
            s_IntListPool.Return(_slashes);
            _slashes = null;
        }

        if (_tokens != null)
        {
            s_TokenListPool.Return(_tokens);
            _tokens = null;
        }

        if (_parts != null)
        {
            s_StringListPool.Return(_parts);
            _parts = null;
        }

        _poolsRented = false;
    }

    private ScanResult ScanInternal()
    {
        while (_index < _length)
        {
            _code = Advance();

            if (_code == c_CharBackwardSlash)
                HandleBackslash();
            else if (_braceEscaped || _code == c_CharLeftCurlyBrace)
                HandleBrace();
            else if (_code == c_CharForwardSlash)
                HandleSlash();
            else if (!_options.NoExt && IsExtglobChar(_code) && Peek() == c_CharLeftParentheses)
                HandleExtglob();
            else if (_code == c_CharAsterisk)
                HandleAsterisk();
            else if (_code == c_CharQuestionMark)
                HandleQuestionMark();
            else if (_code == c_CharLeftSquareBracket)
                HandleBracket();
            else if (!_options.NoNegate && _code == c_CharExclamationMark && _index == _start)
                HandleNegation();
            else if (!_options.NoParen && _code == c_CharLeftParentheses)
                HandleParen();
            else if (_isGlob)
            {
                _finished = true;
                if (!_scanToEnd)
                    break;
            }
        }

        return BuildResult();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsEndOfString()
    {
        return _index >= _length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private char Peek()
    {
        int nextIndex = _index + 1;
        var span = _input.Span;
        return nextIndex < span.Length ? span[nextIndex] : '\0';
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private char Advance()
    {
        _prev = _code;
        var span = _input.Span;
        return ++_index < span.Length ? (_code = span[_index]) : '\0';
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsPathSeparator(char code)
    {
        return code is c_CharForwardSlash or c_CharBackwardSlash;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsExtglobChar(char code)
    {
        return code is c_CharPlus or c_CharAt or c_CharAsterisk or c_CharQuestionMark or c_CharExclamationMark;
    }

    private void HandleBackslash()
    {
        _backslashes = _token.Backslashes = true;
        _code = Advance();

        if (_code == c_CharLeftCurlyBrace)
            _braceEscaped = true;
    }

    private void HandleBrace()
    {
        _braces++;

        while (!IsEndOfString() && (_code = Advance()) != '\0')
        {
            if (_code == c_CharBackwardSlash)
            {
                _backslashes = _token.Backslashes = true;
                Advance();
                continue;
            }

            if (_code == c_CharLeftCurlyBrace)
            {
                _braces++;
                continue;
            }

            if (!_braceEscaped && _code == c_CharDot && (_code = Advance()) == c_CharDot)
            {
                _isBrace = _token.IsBrace = true;
                _isGlob = _token.IsGlob = true;
                _finished = true;

                if (_scanToEnd)
                    continue;

                break;
            }

            if (!_braceEscaped && _code == c_CharComma)
            {
                _isBrace = _token.IsBrace = true;
                _isGlob = _token.IsGlob = true;
                _finished = true;

                if (_scanToEnd)
                    continue;

                break;
            }

            if (_code == c_CharRightCurlyBrace)
            {
                _braces--;

                if (_braces == 0)
                {
                    _braceEscaped = false;
                    _isBrace = _token.IsBrace = true;
                    _finished = true;
                    break;
                }
            }
        }

        if (!_scanToEnd)
            _index = _length + 1; // Force exit from main loop
    }

    private void HandleSlash()
    {
        _slashes!.Add(_index);
        _tokens!.Add(_token);
        _token = new Token { Value = "", IsGlob = false };

        if (_finished)
            return;

        if (_prev == c_CharDot && _index == (_start + 1))
        {
            _start += 2;
            return;
        }

        _lastIndex = _index + 1;
    }

    private void HandleExtglob()
    {
        _isGlob = _token.IsGlob = true;
        _isExtglob = _token.IsExtglob = true;
        _finished = true;

        if (_code == c_CharExclamationMark && _index == _start)
            _negatedExtglob = true;

        if (_scanToEnd)
        {
            while (!IsEndOfString() && (_code = Advance()) != '\0')
            {
                if (_code == c_CharBackwardSlash)
                {
                    _backslashes = _token.Backslashes = true;
                    _code = Advance();
                    continue;
                }

                if (_code == c_CharRightParentheses)
                {
                    _isGlob = _token.IsGlob = true;
                    _finished = true;
                    break;
                }
            }
        }
        else
            _index = _length + 1; // Force exit from main loop
    }

    private void HandleAsterisk()
    {
        if (_prev == c_CharAsterisk)
            _isGlobstar = _token.IsGlobstar = true;

        _isGlob = _token.IsGlob = true;
        _finished = true;

        if (!_scanToEnd)
            _index = _length + 1; // Force exit from main loop
    }

    private void HandleQuestionMark()
    {
        _isGlob = _token.IsGlob = true;
        _finished = true;

        if (!_scanToEnd)
            _index = _length + 1; // Force exit from main loop
    }

    private void HandleBracket()
    {
        while (!IsEndOfString())
        {
            char next = Advance();
            if (next == '\0')
                break;

            if (next == c_CharBackwardSlash)
            {
                _backslashes = _token.Backslashes = true;
                Advance();
                continue;
            }

            if (next == c_CharRightSquareBracket)
            {
                _isBracket = _token.IsBracket = true;
                _isGlob = _token.IsGlob = true;
                _finished = true;
                break;
            }
        }

        if (!_scanToEnd)
            _index = _length + 1; // Force exit from main loop
    }

    private void HandleNegation()
    {
        _negated = _token.Negated = true;
        _start++;
    }

    private void HandleParen()
    {
        _isGlob = _token.IsGlob = true;

        if (_scanToEnd)
        {
            while (!IsEndOfString() && (_code = Advance()) != '\0')
            {
                if (_code == c_CharLeftParentheses)
                {
                    _backslashes = _token.Backslashes = true;
                    _code = Advance();
                    continue;
                }

                if (_code == c_CharRightParentheses)
                {
                    _finished = true;
                    break;
                }
            }
        }
        else
            _index = _length + 1; // Force exit from main loop
    }

    private ScanResult BuildResult()
    {
        if (_options.NoExt)
        {
            _isExtglob = false;
            _isGlob = false;
        }

        var inputSpan = _input.Span;
        var workingSpan = inputSpan[_start..];
        string prefix = "";
        string baseStr;
        string glob = "";

        if (_start > 0)
        {
            prefix = inputSpan[.._start].ToString();
            _lastIndex -= _start;
        }

        if (workingSpan.Length > 0 && _isGlob && _lastIndex > 0)
        {
            baseStr = workingSpan[.._lastIndex].ToString();
            glob = workingSpan[_lastIndex..].ToString();
        }
        else if (_isGlob)
        {
            baseStr = "";
            glob = workingSpan.ToString();
        }
        else
        {
            baseStr = workingSpan.ToString();
        }

        // Trim trailing path separator from base
        if (baseStr.Length > 0 && baseStr != "/" && baseStr.Length < inputSpan.Length)
        {
            if (IsPathSeparator(baseStr[^1]))
                baseStr = baseStr[..^1];
        }

        if (_options.Unescape)
        {
            if (glob.Length > 0)
                glob = Utils.RemoveBackslashes(glob);

            if (baseStr.Length > 0 && _backslashes)
                baseStr = Utils.RemoveBackslashes(baseStr);
        }

        var state = new ScanResult
        {
            Prefix = prefix,
            Input = _input.ToString(),
            Start = _start,
            Base = baseStr,
            Glob = glob,
            IsBrace = _isBrace,
            IsBracket = _isBracket,
            IsGlob = _isGlob,
            IsExtglob = _isExtglob,
            IsGlobstar = _isGlobstar,
            Negated = _negated,
            NegatedExtglob = _negatedExtglob
        };

        if (_options.Tokens)
        {
            state.MaxDepth = 0;
            if (!IsPathSeparator(_code))
                _tokens!.Add(_token);

            // Transfer tokens to a new list (owned by ScanResult)
            state.Tokens = [.. _tokens!];
        }

        if (_options.Parts || _options.Tokens)
        {
            BuildParts(state, prefix);
        }

        return state;
    }

    private void BuildParts(ScanResult state, string prefix)
    {
        int? prevIndex = null;
        var inputSpan = _input.Span;

        for (int idx = 0; idx < _slashes!.Count; idx++)
        {
            int n = prevIndex.HasValue ? prevIndex.Value + 1 : _start;
            int i = _slashes[idx];

            var valueSpan = inputSpan[n..i];

            if (_options.Tokens && idx < _tokens!.Count)
            {
                if (idx == 0 && _start != 0)
                {
                    _tokens[idx].IsPrefix = true;
                    _tokens[idx].Value = prefix;
                }
                else
                {
                    _tokens![idx].Value = valueSpan.ToString();
                }

                SetDepth(_tokens![idx]);
            }

            if (idx != 0 || valueSpan.Length > 0)
                _parts!.Add(valueSpan.ToString());

            prevIndex = i;
        }

        if (prevIndex.HasValue && prevIndex.Value + 1 < inputSpan.Length)
        {
            var valueSpan = inputSpan[(prevIndex.Value + 1)..];
            string value = valueSpan.ToString();
            _parts!.Add(value);

            if (_options.Tokens && _tokens!.Count > 0)
            {
                _tokens[^1].Value = value;
                SetDepth(_tokens[^1]);
            }
        }

        state.Slashes = [.. _slashes!];
        state.Parts = [.. _parts!];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetDepth(Token token)
    {
        if (token == null)
            return;

        // Depth logic can be extended as needed
    }
}
