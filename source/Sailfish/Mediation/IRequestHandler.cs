using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Mediation;

/// <summary>
///     Handles a <typeparamref name="TRequest" /> and returns a <typeparamref name="TResponse" />. Exactly
///     one handler is expected per request type; registering another for the same request replaces the
///     previous one (last registration wins), which is how a consumer overrides a default.
/// </summary>
/// <typeparam name="TRequest">The request type this handler services.</typeparam>
/// <typeparam name="TResponse">The response type produced.</typeparam>
public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    /// <summary>Handle the request and produce its response.</summary>
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
