using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Mediation;

/// <summary>
///     Sends a request to its single handler and returns the response. This is the narrow "ask for a value"
///     half of the mediator — inject it into a component that issues requests. Components that only raise
///     events inject <see cref="IPublisher" /> instead.
/// </summary>
public interface ISender
{
    /// <summary>Send <paramref name="request" /> to its handler and return the response.</summary>
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
