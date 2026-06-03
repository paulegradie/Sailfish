using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Analysis.Ai;

/// <summary>
///     The <b>advanced</b> seam for the AI "Skipper" — the crewmate that reads the instruments (SailDiff /
///     ScaleFish) and explains what changed and why. An agent owns the whole flow for a session: prompt,
///     model call, and parsing the reply into a <see cref="SkipperReview" />.
///     <para>
///         <b>Most consumers should not implement this.</b> Implement <see cref="ISkipperTransport" /> instead
///         and register it with <c>services.AddSkipperTransport&lt;T&gt;()</c>: Sailfish then owns the intelligence
///         — it assembles a rigorous, grounded prompt (<see cref="ISkipperPromptBuilder" />) from the authoritative
///         numbers and parses the structured reply (<see cref="ISkipperResponseParser" />), leaving you only the
///         model call. Implement <see cref="ISailfishAgent" /> directly only when you need to own prompt-building or
///         parsing as well (e.g. a model with native structured output, or a bespoke agentic protocol).
///     </para>
///     <para>
///         Register a custom implementation from an <c>IRegisterSailfishServices</c> provider:
///         <c>services.AddSingleton&lt;ISailfishAgent, MyAgent&gt;()</c>. When neither an agent nor a transport is
///         registered a no-op default is used and AI analysis is silently skipped — the feature is strictly additive.
///     </para>
/// </summary>
public interface ISailfishAgent
{
    /// <summary>
    ///     Analyze a completed comparison and return a <see cref="SkipperReview" />.
    ///     <para>
    ///         Implementations must not compute or invent measurements: reason only over the grounded figures
    ///         in <see cref="SkipperSession.Context" />, and for any claim about code cite a real
    ///         <c>file:line</c> that was actually read.
    ///     </para>
    /// </summary>
    Task<SkipperReview> RunAsync(SkipperSession session, CancellationToken cancellationToken);
}
