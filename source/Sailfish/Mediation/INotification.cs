namespace Sailfish.Mediation;

/// <summary>
///     Marker interface for a Sailfish notification: a domain event that is broadcast to every
///     registered <see cref="INotificationHandler{TNotification}" />. Publishing is fire-to-all —
///     a notification may have zero, one, or many handlers, and every handler runs.
/// </summary>
/// <remarks>
///     This is Sailfish's own contract; it intentionally has no third-party dependency. Implement
///     <see cref="INotificationHandler{TNotification}" /> to observe one of the framework's events.
/// </remarks>
public interface INotification;
