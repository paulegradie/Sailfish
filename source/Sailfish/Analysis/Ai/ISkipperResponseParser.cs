namespace Sailfish.Analysis.Ai;

/// <summary>
///     Parses a model's raw text reply into a structured <see cref="SkipperReview" />. Framework-owned because it is
///     the twin of the output-schema contract emitted by <see cref="DefaultSkipperPromptBuilder" /> — together they
///     are one serialization contract, and the library that owns <see cref="SkipperReview" /> owns both halves.
///     Consumers implement <see cref="ISkipperTransport" /> (pure transport) and never touch this; it exists as a
///     seam only so an advanced host can pair a custom prompt with a matching parser.
/// </summary>
public interface ISkipperResponseParser
{
    /// <summary>
    ///     Parse <paramref name="modelText" /> into a <see cref="SkipperReview" />. Tolerant by design: a reply that
    ///     isn't the expected JSON degrades to a review carrying the raw text rather than throwing, so a stray model
    ///     never breaks a run.
    /// </summary>
    SkipperReview Parse(string modelText);
}
