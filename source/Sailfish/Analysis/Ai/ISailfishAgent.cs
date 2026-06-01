using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Analysis.Ai;

/// <summary>
///     The single seam a consumer implements to give Sailfish an AI "Skipper" — the crewmate that reads
///     the instruments (SailDiff / ScaleFish) and explains what changed and why.
///     <para>
///         Implementations are pure <b>transport</b>. Sailfish assembles a fully-grounded
///         <see cref="SkipperSession" /> (authoritative numbers + the capabilities the agent is allowed to
///         use) and hands it over. The implementation forwards it to a model however it likes — a one-shot
///         completion, a local model, or a full agentic loop (e.g. the Claude Agent SDK / <c>claude</c> CLI)
///         that uses the read-only code access granted by <see cref="ICodeReadCapability" /> to investigate
///         the code under test.
///     </para>
///     <para>
///         Register a custom implementation from an <c>IRegisterSailfishServices</c> provider:
///         <c>services.AddSingleton&lt;ISailfishAgent, MyAgent&gt;()</c>. When none is registered a no-op
///         default is used and AI analysis is silently skipped — the feature is strictly additive.
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
