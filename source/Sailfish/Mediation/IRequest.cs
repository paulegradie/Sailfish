namespace Sailfish.Mediation;

/// <summary>
///     Marker interface for a request that produces a <typeparamref name="TResponse" />. Unlike a
///     <see cref="INotification" />, a request is handled by exactly one
///     <see cref="IRequestHandler{TRequest,TResponse}" /> and returns a value to the caller. Use this for a
///     swappable, single-implementation operation (e.g. "locate the before/after tracking files").
/// </summary>
/// <typeparam name="TResponse">The response the request's handler returns.</typeparam>
public interface IRequest<out TResponse>;
