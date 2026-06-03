using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Analysis.Ai;

/// <summary>
///     Defensive default <see cref="ISkipperTransport" />, registered via <c>TryAdd</c> so that
///     <see cref="PromptDrivenSailfishAgent" /> always has a transport to resolve. It is never meant to run: the
///     agent short-circuits to <see cref="SkipperReview.Empty" /> when the transport is this no-op, so a consumer
///     who wired the pipeline without supplying a real transport degrades silently rather than erroring.
/// </summary>
internal sealed class NoOpSkipperTransport : ISkipperTransport
{
    public Task<string> CompleteAsync(string prompt, SkipperSession session, CancellationToken cancellationToken)
    {
        return Task.FromResult(string.Empty);
    }
}
