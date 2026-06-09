using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Mediation;

/// <summary>
///     Handles a published <typeparamref name="TNotification" />. A notification may be observed by any
///     number of handlers; the publisher invokes them all, isolates a throwing handler from its siblings,
///     and aggregates failures (see <see cref="IPublisher" />).
/// </summary>
/// <remarks>
///     Registering a handler <em>adds</em> to any handlers already registered for the same notification — it
///     never replaces them, and every registered handler runs. To replace a framework default you must remove
///     its registration from the service collection; you cannot override it simply by registering your own.
///     (Contrast <see cref="IRequestHandler{TRequest,TResponse}" />, where exactly one handler runs and the
///     last registration wins.)
/// </remarks>
/// <typeparam name="TNotification">The notification type this handler observes.</typeparam>
public interface INotificationHandler<in TNotification> where TNotification : INotification
{
    /// <summary>Handle the notification.</summary>
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}
