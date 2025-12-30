using Snowberry.Globbing.Models;

namespace Snowberry.Globbing;

/// <summary>
/// Represents a method that tests if an input string matches a glob pattern.
/// </summary>
/// <param name="input">The input <see cref="string"/> to test against the pattern.</param>
/// <returns><see langword="true"/> if the input matches the pattern; otherwise, <see langword="false"/>.</returns>
public delegate bool MatcherHandler(string input);

/// <summary>
/// Represents a method that expands a range expression in brace patterns.
/// </summary>
/// <param name="range">An array of strings representing the range values to expand.</param>
/// <param name="options">The <see cref="GlobbingOptions"/> instance containing configuration settings.</param>
/// <returns>A <see cref="string"/> containing the expanded range.</returns>
public delegate string ExpandRangeHandler(string[] range, GlobbingOptions options);

/// <summary>
/// Represents a method that formats an input string before pattern matching.
/// </summary>
/// <param name="input">The input <see cref="string"/> to format.</param>
/// <returns>A formatted <see cref="string"/>.</returns>
public delegate string FormatHandler(string input);

/// <summary>
/// Represents a method that is called when an input is ignored during matching.
/// </summary>
/// <param name="result">The <see cref="MatchResult"/> containing information about the ignored match.</param>
public delegate void IgnoreHandler(MatchResult result);

/// <summary>
/// Represents a method that is called when an input successfully matches a pattern.
/// </summary>
/// <param name="result">The <see cref="MatchResult"/> containing information about the successful match.</param>
public delegate void MatchHandler(MatchResult result);

/// <summary>
/// Represents a method that is called for every input processed, regardless of match status.
/// </summary>
/// <param name="result">The <see cref="MatchResult"/> containing information about the processed input.</param>
public delegate void ResultHandler(MatchResult result);
