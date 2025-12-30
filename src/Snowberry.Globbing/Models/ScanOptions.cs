namespace Snowberry.Globbing.Models;

/// <summary>
/// Configuration options for controlling glob pattern scanning behavior.
/// </summary>
/// <remarks>
/// These options determine what information is extracted during the scan operation and how the pattern is processed.
/// </remarks>
public class ScanOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to extract and return path parts (segments).
    /// </summary>
    /// <value><see langword="true"/> to extract parts; otherwise, <see langword="false"/>.</value>
    public bool Parts { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to scan the entire pattern to the end.
    /// </summary>
    /// <value><see langword="true"/> to scan to end; otherwise, <see langword="false"/>.</value>
    public bool ScanToEnd { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to extract and return tokens.
    /// </summary>
    /// <value><see langword="true"/> to extract tokens; otherwise, <see langword="false"/>.</value>
    public bool Tokens { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to disable extglob support during scanning.
    /// </summary>
    /// <value><see langword="true"/> to disable extglobs; otherwise, <see langword="false"/>.</value>
    public bool NoExt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to disable negation pattern support during scanning.
    /// </summary>
    /// <value><see langword="true"/> to disable negation; otherwise, <see langword="false"/>.</value>
    public bool NoNegate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to disable parenthesis matching during scanning.
    /// </summary>
    /// <value><see langword="true"/> to disable paren matching; otherwise, <see langword="false"/>.</value>
    public bool NoParen { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to unescape backslash sequences during scanning.
    /// </summary>
    /// <value><see langword="true"/> to unescape; otherwise, <see langword="false"/>.</value>
    public bool Unescape { get; set; }
}
