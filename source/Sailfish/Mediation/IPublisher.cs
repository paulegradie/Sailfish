using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Mediation;

/// <summary>
///     Publishes notifications to their handlers. This is the narrow "broadcast an event" half of the
///     mediator — inject it into a component that only raises events (most of the framework). Components
///     that issue requests inject <see cref="ISender" /> instead; a component that does both injects both.
/// </summary>
/// <remarks>
///     The default publisher runs every registered handler in registration order, isolates a throwing
///     handler so its siblings still run, logs each failure, and — once all handlers have run — throws an
///     <see cref="System.AggregateException" /> if any failed (fail-loud). <see cref="System.OperationCanceledException" />
///     propagates immediately and is not treated as a handler failure.
/// </remarks>
public interface IPublisher
{
    /// <summary>Publish <paramref name="notification" /> to every registered handler for its runtime type.</summary>
    Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}
