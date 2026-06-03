using System;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Logging;

namespace Sailfish.Analysis.Ai;

/// <summary>
///     The framework's default <see cref="ISailfishAgent" />: it builds the rigorous, grounded prompt
///     (<see cref="ISkipperPromptBuilder" />), hands it to the consumer-supplied <see cref="ISkipperTransport" />, and
///     parses the reply back into a <see cref="SkipperReview" /> (<see cref="ISkipperResponseParser" />). This is the
///     "Sailfish owns the intelligence, you own the transport" split made real — a consumer registers only a
///     transport via <c>services.AddSkipperTransport&lt;T&gt;()</c> and gets all three for free.
///     <para>
///         It stays invisible on failure: when no real transport is wired it short-circuits to
///         <see cref="SkipperReview.Empty" />, and any transport / parse error degrades to empty (and is logged) so a
///         missing or offline model never breaks a benchmark run.
///     </para>
/// </summary>
internal sealed class PromptDrivenSailfishAgent : ISailfishAgent
{
    private readonly ISkipperPromptBuilder promptBuilder;
    private readonly ISkipperResponseParser responseParser;
    private readonly ISkipperTransport transport;
    private readonly ILogger logger;

    public PromptDrivenSailfishAgent(
        ISkipperPromptBuilder promptBuilder,
        ISkipperTransport transport,
        ISkipperResponseParser responseParser,
        ILogger logger)
    {
        this.promptBuilder = promptBuilder;
        this.transport = transport;
        this.responseParser = responseParser;
        this.logger = logger;
    }

    public async Task<SkipperReview> RunAsync(SkipperSession session, CancellationToken cancellationToken)
    {
        // No real transport registered — the pipeline is wired but inert. Stay invisible.
        if (transport is NoOpSkipperTransport)
        {
            return SkipperReview.Empty;
        }

        try
        {
            var prompt = promptBuilder.Build(session);
            var modelText = await transport.CompleteAsync(prompt, session, cancellationToken).ConfigureAwait(false);
            return responseParser.Parse(modelText);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The run is being cancelled (e.g. host shutdown) — propagate rather than masking it as "no analysis".
            throw;
        }
        catch (Exception ex)
        {
            // Skipper is strictly additive: a missing / offline / slow model must never throw into a run.
            logger.Log(LogLevel.Warning, ex, "Skipper transport failed; continuing without AI analysis.");
            return SkipperReview.Empty;
        }
    }
}
