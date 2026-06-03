using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Analysis.Ai;

/// <summary>
///     The thin seam most consumers implement: <b>pure transport</b>. Sailfish owns the intelligence — it builds a
///     rigorous, grounded prompt (<see cref="ISkipperPromptBuilder" />) from the authoritative SailDiff / ScaleFish
///     numbers, and parses the model's reply back into a <see cref="SkipperReview" />
///     (<see cref="ISkipperResponseParser" />). All a transport does is send the prompt to a model and return its
///     raw text.
///     <para>
///         The model is free to be a one-shot completion, a local model, or a full agentic loop (e.g. the
///         <c>claude</c> CLI / Claude Agent SDK) that uses the read-only code access granted by
///         <see cref="ICodeReadCapability" /> to investigate the code under test. The <see cref="SkipperSession" />
///         carries the repository root and granted capabilities so the transport can scope that access correctly.
///     </para>
///     <para>
///         Register one with <c>services.AddSkipperTransport&lt;MyTransport&gt;()</c> from an
///         <c>IRegisterSailfishServices</c> provider — that single call also wires in the framework's
///         <see cref="ISailfishAgent" /> pipeline. A consumer who needs to own prompt-building or parsing as well can
///         instead implement the lower-level <see cref="ISailfishAgent" /> directly.
///     </para>
/// </summary>
public interface ISkipperTransport
{
    /// <summary>
    ///     Send <paramref name="prompt" /> to a model and return its raw text reply. The framework parses that text;
    ///     a transport never needs to understand the prompt or the response schema. Implementations should surface a
    ///     missing / offline / timed-out model by throwing — the framework treats any failure as "Skipper
    ///     unavailable" and stays invisible (it never breaks a benchmark run).
    /// </summary>
    Task<string> CompleteAsync(string prompt, SkipperSession session, CancellationToken cancellationToken);
}
