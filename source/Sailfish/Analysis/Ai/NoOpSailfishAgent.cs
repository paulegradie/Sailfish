using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Analysis.Ai;

/// <summary>
///     Default <see cref="ISailfishAgent" />, registered when the consumer has not supplied one. Returns an
///     empty review so the AI analysis layer stays completely invisible until a real agent is registered via
///     <c>IRegisterSailfishServices</c>.
/// </summary>
internal sealed class NoOpSailfishAgent : ISailfishAgent
{
    public Task<SkipperReview> RunAsync(SkipperSession session, CancellationToken cancellationToken)
    {
        return Task.FromResult(SkipperReview.Empty);
    }
}
