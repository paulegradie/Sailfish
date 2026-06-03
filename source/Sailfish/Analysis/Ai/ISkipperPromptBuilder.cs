namespace Sailfish.Analysis.Ai;

/// <summary>
///     Assembles the rigorous, grounded analysis prompt handed to an <see cref="ISkipperTransport" />. Shipped by the
///     framework (<see cref="DefaultSkipperPromptBuilder" />) so every consumer gets the same disciplined framing —
///     "these numbers are authoritative, explain the why, cite <c>file:line</c>" — and the same output-schema
///     contract that <see cref="ISkipperResponseParser" /> reads back. Extend it by registering
///     <see cref="ISkipperPromptSection" />s rather than replacing it; replace it only when you need wholly different
///     framing.
/// </summary>
public interface ISkipperPromptBuilder
{
    /// <summary>Build the full prompt for one analysis from the grounded <paramref name="session" />.</summary>
    string Build(SkipperSession session);
}
