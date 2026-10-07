namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for POSIX character classes ported from picomatch.
/// POSIX classes are expanded with default options.
/// </summary>
public class PosixClassesTests
{
    private static readonly GlobOptions s_StrictSlashes = new() { StrictSlashes = true };

    [Theory]
    [InlineData("a", "[[:alnum:]]", true)]
    [InlineData("Z", "[[:alnum:]]", true)]
    [InlineData("9", "[[:alnum:]]", true)]
    [InlineData("!", "[[:alnum:]]", false)]
    [InlineData(".", "[[:alnum:]]", false)]
    public void AlnumClassShouldMatchAlphanumericCharacters(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("abc123", "[[:alnum:]]+", true)]
    [InlineData("abc", "[[:alnum:]]+", true)]
    [InlineData("123", "[[:alnum:]]+", true)]
    [InlineData("a-b", "[[:alnum:]]+", false)]
    public void AlnumClassWithPlusShouldMatch(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", "[[:alpha:]]", true)]
    [InlineData("b", "[[:alpha:]]", true)]
    [InlineData("Z", "[[:alpha:]]", true)]
    [InlineData("1", "[[:alpha:]]", false)]
    [InlineData("9", "[[:alpha:]]", false)]
    [InlineData("!", "[[:alpha:]]", false)]
    public void AlphaClassShouldMatchAlphabeticCharacters(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", "[[:alpha:]123]", true)]
    [InlineData("1", "[[:alpha:]123]", true)]
    [InlineData("5", "[[:alpha:]123]", false)]
    [InlineData("A", "[[:alpha:]123]", true)]
    public void AlphaClassWithExtraChars(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("abc", "[[:alpha:]]*", true)]
    [InlineData("abc123", "[[:alpha:]]*", true)]
    [InlineData("123", "[[:alpha:]]*", false)]
    public void AlphaClassWithStarShouldMatch(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", "[[:alpha:][:digit:]]", true)]
    [InlineData("1", "[[:alpha:][:digit:]]", true)]
    [InlineData("!", "[[:alpha:][:digit:]]", false)]
    [InlineData("3", "[[:alpha:][:digit:]]", true)]
    [InlineData("aa", "[[:alpha:][:digit:]]", false)]
    [InlineData("a3", "[[:alpha:][:digit:]]", false)]
    public void AlphaDigitCombinedClass(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a b", "a (\\[b\\]|[b])", true)]
    [InlineData("a [b]", "a (\\[b\\]|[b])", true)]
    [InlineData("a c", "a (\\[b\\]|[b])", false)]
    public void AlternationWithBrackets(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", "[[:ascii:]]", true)]
    [InlineData("1", "[[:ascii:]]", true)]
    [InlineData("\x7F", "[[:ascii:]]", true)]
    [InlineData("\u00E9", "[[:ascii:]]", false)]
    public void AsciiClassShouldMatchAsciiCharacters(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData(" ", "[[:blank:]]", true)]
    [InlineData("\t", "[[:blank:]]", true)]
    [InlineData("a", "[[:blank:]]", false)]
    [InlineData("\n", "[[:blank:]]", false)]
    public void BlankClassShouldMatchSpaceAndTab(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a [b]", "a ([b])", true)]
    [InlineData("a b", "a ([b])", true)]
    [InlineData("a c", "a ([b])", false)]
    public void BracketsInParens(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", "[[:alpha:]]", true)]
    [InlineData("1", "[[:alpha:]]", false)]
    [InlineData("a]", "[[:alpha:]]", false)]
    [InlineData("7", "[[:digit:]]", true)]
    [InlineData("a", "[[:digit:]]", false)]
    [InlineData("a", "[[:alpha:][:digit:]]", true)]
    [InlineData("7", "[[:alpha:][:digit:]]", true)]
    [InlineData("-", "[[:alpha:][:digit:]]", false)]
    public void ClassesShouldBeExpandedWithDefaultOptions(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("[[:alpha:]]")]
    [InlineData("[[:alpha:][:digit:]]")]
    [InlineData("[![:alpha:]]")]
    [InlineData("*[[:space:]]*")]
    public void ClassesShouldNotLeakIntoGeneratedRegex(string pattern)
    {
        // Portability: a nested "[:" is a class in PostgreSQL but plain text in .NET and JavaScript.
        new Glob(pattern).ToRegexString().Should().NotContain("[:");
    }

    [Theory]
    [InlineData("\x00", "[[:cntrl:]]", true)]
    [InlineData("\x1F", "[[:cntrl:]]", true)]
    [InlineData("\x7F", "[[:cntrl:]]", true)]
    [InlineData("a", "[[:cntrl:]]", false)]
    [InlineData(" ", "[[:cntrl:]]", false)]
    public void CntrlClassShouldMatchControlCharacters(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a1", "[[:alpha:]][[:digit:]]", true)]
    [InlineData("Z9", "[[:alpha:]][[:digit:]]", true)]
    [InlineData("1a", "[[:alpha:]][[:digit:]]", false)]
    [InlineData("aa", "[[:alpha:]][[:digit:]]", false)]
    public void CombinedPosixClassesShouldMatch(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("0", "[[:digit:]]", true)]
    [InlineData("1", "[[:digit:]]", true)]
    [InlineData("9", "[[:digit:]]", true)]
    [InlineData("a", "[[:digit:]]", false)]
    [InlineData("A", "[[:digit:]]", false)]
    [InlineData("X", "[[:digit:]]", false)]
    public void DigitClassShouldMatchDigits(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("123", "[[:digit:]]*", true)]
    [InlineData("123abc", "[[:digit:]]*", true)]
    [InlineData("abc", "[[:digit:]]*", false)]
    public void DigitClassWithStarShouldMatch(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData(".", "[[:digit:][:punct:][:space:]]", true)]
    [InlineData("a", "[[:digit:][:punct:][:space:]]", false)]
    [InlineData("!", "[[:digit:][:punct:][:space:]]", true)]
    [InlineData("!", "[[:digit:]][[:punct:]][[:space:]]", false)]
    [InlineData("1! ", "[[:digit:]][[:punct:]][[:space:]]", true)]
    [InlineData("1!  ", "[[:digit:]][[:punct:]][[:space:]]", false)]
    public void DigitPunctSpaceCombinations(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a [b]", "a \\[b\\]", true)]
    [InlineData("a b", "a \\[b\\]", false)]
    public void EscapedBracketsMatchLiteral(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", "[[:graph:]]", true)]
    [InlineData("A", "[[:graph:]]", true)]
    [InlineData("!", "[[:graph:]]", true)]
    [InlineData(" ", "[[:graph:]]", false)]
    public void GraphClassShouldMatchVisibleCharacters(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", "[:alpha:]", true)]
    [InlineData("l", "[:alpha:]", true)]
    [InlineData(":", "[:alpha:]", true)]
    [InlineData("b", "[:alpha:]", false)]
    public void InvalidPosixBracketIsJustCharClass(string input, string pattern, bool expected)
    {
        // invalid posix bracket, but valid char class - matches literal chars :, a, l, p, h
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", "[:al:]", true)]
    [InlineData("b", "[:al:]", false)]
    // NOTE: Unclosed bracket "[[:al:]" has different behavior across .NET versions
    // .NET 9/10 treats it as literal match, .NET Framework 4.8 handles it differently
    // [InlineData("a", "[[:al:]", true)]
    [InlineData("!", "[abc[:punct:][0-9]", true)]
    public void InvalidPosixExpressionsAreCharsToMatch(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a [b]", "a [b]", true)]
    [InlineData("a b", "a [b]", true)]
    [InlineData("a [b] c", "a [b] c", true)]
    [InlineData("a b c", "a [b] c", true)]
    [InlineData("a c", "a [b]", false)]
    public void LiteralBracketsInPatterns(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", "[[:lower:]]", true)]
    [InlineData("z", "[[:lower:]]", true)]
    [InlineData("m", "[[:lower:]]", true)]
    [InlineData("A", "[[:lower:]]", false)]
    [InlineData("Z", "[[:lower:]]", false)]
    [InlineData("9", "[[:lower:]]", false)]
    public void LowerClassShouldMatchLowercaseLetters(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("abc", "[[:lower:]]+", true)]
    [InlineData("ABC", "[[:lower:]]+", false)]
    [InlineData("aBc", "[[:lower:]]+", false)]
    public void LowerClassWithPlusShouldMatch(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("aA", "[[:lower:]][[:upper:]]", true)]
    [InlineData("AA", "[[:lower:]][[:upper:]]", false)]
    [InlineData("Aa", "[[:lower:]][[:upper:]]", false)]
    [InlineData("aB", "[[:lower:]][[:upper:]]", true)]
    public void LowerUpperCombination(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("_", "[[:alnum:][:alpha:][:blank:][:cntrl:][:digit:][:graph:][:lower:][:print:][:punct:][:space:][:upper:][:xdigit:]]", true)]
    [InlineData(".", "[^[:alnum:][:alpha:][:blank:][:cntrl:][:digit:][:lower:][:space:][:upper:][:xdigit:]]", true)]
    [InlineData(".", "[[:alnum:][:alpha:][:blank:][:cntrl:][:digit:][:lower:][:space:][:upper:][:xdigit:]]", false)]
    public void ManyPosixClassesCombined(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("9", "[[:lower:][:digit:]]", true)]
    [InlineData("a", "[[:lower:][:digit:]]", true)]
    [InlineData("A", "[[:lower:][:digit:]]", false)]
    [InlineData("aa", "[[:lower:][:digit:]]", false)]
    [InlineData("9A", "[[:lower:][:digit:]]", false)]
    public void MultiplePosixClassesInOneBracket(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("aa", "[[:lower:][:digit:]]+", true)]
    [InlineData("99", "[[:lower:][:digit:]]+", true)]
    [InlineData("9a", "[[:lower:][:digit:]]+", true)]
    [InlineData("aA", "[[:lower:][:digit:]]+", false)]
    [InlineData("9A", "[[:lower:][:digit:]]+", false)]
    public void MultiplePosixClassesWithPlus(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", "[[:lower:][:digit:]]*", true)]
    [InlineData("A", "[[:lower:][:digit:]]*", false)]
    [InlineData("AA", "[[:lower:][:digit:]]*", false)]
    [InlineData("aa", "[[:lower:][:digit:]]*", true)]
    [InlineData("999", "[[:lower:][:digit:]]*", true)]
    public void MultiplePosixClassesWithStar(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("A", "[^[:alpha:]]", false)]
    [InlineData("9", "[^[:alpha:]]", true)]
    [InlineData("b", "[^[:alpha:]]", false)]
    public void NegatedAlphaWithCaret(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("A", "[![:alpha:]]", false)]
    [InlineData("9", "[![:alpha:]]", true)]
    [InlineData("b", "[![:alpha:]]", false)]
    public void NegatedAlphaWithExclamation(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("A", "[^[:digit:]]", true)]
    [InlineData("9", "[^[:digit:]]", false)]
    [InlineData("b", "[^[:digit:]]", true)]
    public void NegatedDigitClass(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("A", "[![:digit:]]", true)]
    [InlineData("9", "[![:digit:]]", false)]
    [InlineData("b", "[![:digit:]]", true)]
    public void NegatedDigitClassWithExclamation(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("5", "[a-c[:digit:]x-z]", true)]
    [InlineData("b", "[a-c[:digit:]x-z]", true)]
    [InlineData("y", "[a-c[:digit:]x-z]", true)]
    [InlineData("q", "[a-c[:digit:]x-z]", false)]
    public void PosixClassWithRanges(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a.txt", "[[:alpha:]].*", true)]
    [InlineData("1.txt", "[[:alpha:]].*", false)]
    public void PosixClassWithWildcardShouldMatch(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", "[[:print:]]", true)]
    [InlineData("1", "[[:print:]]", true)]
    [InlineData(" ", "[[:print:]]", true)]
    [InlineData("\x7F", "[[:print:]]", false)]
    public void PrintClassShouldMatchPrintableCharacters(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("!", "[[:punct:]]", true)]
    [InlineData("?", "[[:punct:]]", true)]
    [InlineData(";", "[[:punct:]]", true)]
    [InlineData("a", "[[:punct:]]", false)]
    [InlineData("1", "[[:punct:]]", false)]
    [InlineData(" ", "[[:punct:]]", false)]
    public void PunctClassShouldMatchPunctuation(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("?*+", "[[:punct:]]", false)]
    public void PunctClassShouldOnlyMatchOneChar(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("?*+", "[[:punct:]]*", true)]
    [InlineData("|++", "[[:punct:]]*", true)]
    [InlineData("foo?*+", "foo[[:punct:]]*", true)]
    // NOTE: "foo" matching "foo[[:punct:]]*" returns false - star after POSIX class requires at least one match
    public void PunctClassWithStarShouldMatchMultiple(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData(" ", "[[:space:]]", true)]
    [InlineData("\t", "[[:space:]]", true)]
    [InlineData("a", "[[:space:]]", false)]
    [InlineData("1", "[[:space:]]", false)]
    public void SpaceClassShouldMatchWhitespace(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("A", "[[:digit:][:upper:][:space:]]", true)]
    [InlineData("1", "[[:digit:][:upper:][:space:]]", true)]
    [InlineData(" ", "[[:digit:][:upper:][:space:]]", true)]
    [InlineData("a", "[[:digit:][:upper:][:space:]]", false)]
    [InlineData(".", "[[:digit:][:upper:][:space:]]", false)]
    public void ThreePosixClassesCombined(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a1B", "[[:alpha:]][[:digit:]][[:upper:]]", true)]
    [InlineData("a1b", "[[:alpha:]][[:digit:]][[:upper:]]", false)]
    public void ThreeSequentialPosixClasses(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("A", "[[:upper:]]", true)]
    [InlineData("Z", "[[:upper:]]", true)]
    [InlineData("M", "[[:upper:]]", true)]
    [InlineData("a", "[[:upper:]]", false)]
    [InlineData("z", "[[:upper:]]", false)]
    [InlineData("2", "[[:upper:]]", false)]
    public void UpperClassShouldMatchUppercaseLetters(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("ABC", "[[:upper:]]+", true)]
    [InlineData("abc", "[[:upper:]]+", false)]
    [InlineData("aBc", "[[:upper:]]+", false)]
    public void UpperClassWithPlusShouldMatch(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("PATH", "[_[:alpha:]][_[:alnum:]]*", true)]
    [InlineData("P-ATH", "[_[:alpha:]][_[:alnum:]]*", false)]
    public void ValidShIdentifierFirstTwoChars(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("PATH", "[_[:alpha:]]*", true)]
    [InlineData("1PATH", "[_[:alpha:]]*", false)]
    public void ValidShIdentifierStart(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("file1.txt", "*[[:digit:]].*", true)]
    [InlineData("file.txt", "*[[:digit:]].*", false)]
    public void WildcardWithPosixClassShouldMatch(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a c", "a[[:word:]]+c", false)]
    [InlineData("a.c", "a[[:word:]]+c", false)]
    [InlineData("axy.zc", "a[[:word:]]+c", false)]
    [InlineData("a123c", "a[[:word:]]+c", true)]
    [InlineData("a1c", "a[[:word:]]+c", true)]
    [InlineData("abc", "a[[:word:]]+c", true)]
    public void WordClassPatternMatching(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", "[[:word:]]", true)]
    [InlineData("b", "[[:word:]]", true)]
    [InlineData("_", "[[:word:]]", true)]
    [InlineData("!", "[[:word:]]", false)]
    [InlineData(" ", "[[:word:]]", false)]
    public void WordClassShouldMatchWordCharacters(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a c", "a[[:word:]]+", false)]
    [InlineData("a.c", "a[[:word:]]+", false)]
    [InlineData("axy.zc", "a[[:word:]]+", false)]
    [InlineData("a123c", "a[[:word:]]+", true)]
    [InlineData("a1c", "a[[:word:]]+", true)]
    [InlineData("axyzc", "a[[:word:]]+", true)]
    public void WordClassWithPlusPatternMatching(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("hello_world123", "[[:word:]]+", true)]
    [InlineData("hello world", "[[:word:]]+", false)]
    public void WordClassWithPlusShouldMatch(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("e", "[[:xdigit:]]", true)]
    [InlineData("0", "[[:xdigit:]]", true)]
    [InlineData("F", "[[:xdigit:]]", true)]
    [InlineData("g", "[[:xdigit:]]", false)]
    [InlineData("G", "[[:xdigit:]]", false)]
    [InlineData("!", "[[:xdigit:]]", false)]
    public void XdigitClassShouldMatchHexDigits(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("deadbeef", "[[:xdigit:]]+", true)]
    [InlineData("DEADBEEF", "[[:xdigit:]]+", true)]
    [InlineData("123abc", "[[:xdigit:]]+", true)]
    [InlineData("xyz", "[[:xdigit:]]+", false)]
    public void XdigitClassWithPlusShouldMatch(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("ababab", "[[:xdigit:]]*", true)]
    [InlineData("020202", "[[:xdigit:]]*", true)]
    [InlineData("900", "[[:xdigit:]]*", true)]
    [InlineData("xyz", "[[:xdigit:]]*", false)]
    public void XdigitWithStarShouldMatchHexStrings(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }
}