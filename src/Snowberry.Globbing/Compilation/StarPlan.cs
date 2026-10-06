namespace Snowberry.Globbing.Compilation;

/// <summary>
/// How a star run is written, and which nodes it consumes.
/// </summary>
/// <param name="Form">The regex form.</param>
/// <param name="Last">The index of the last node the star run consumes: the last of any merged <c>**</c> runs, or, for <see cref="StarForm.MiddleGlobstar"/> and <see cref="StarForm.LeadingGlobstar"/>, the separator that follows it.</param>
/// <param name="BeforeLast">The index of the node before <paramref name="Last"/>, or a negative value if there is none.</param>
/// <param name="SegmentStart">
/// Whether the star starts a path segment and so gets a dot guard; for <see cref="StarForm.LeadingGlobstar"/>, whether it starts an anchored
/// pattern and may use <see cref="RegexFragments.LeadingGlobstar"/> instead of <see cref="RegexFragments.AlternativeLeadingGlobstar"/>.
/// </param>
/// <param name="AfterLeadingDot">Whether the star follows a dot that starts a path segment, so it may not match an empty or <c>.</c> remainder.</param>
/// <param name="OneChar">Whether a single <c>*</c> that starts a segment or follows a leading dot requires one more character, as a lookahead.</param>
/// <param name="MoreAfter">Whether a <see cref="StarForm.MiddleGlobstar"/> is followed by more nodes or sits in a nested sequence, which adds an end-of-input alternative.</param>
internal readonly record struct StarPlan(
    StarForm Form,
    int Last,
    int BeforeLast,
    bool SegmentStart = false,
    bool AfterLeadingDot = false,
    bool OneChar = false,
    bool MoreAfter = false);
