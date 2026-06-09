using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Sailfish.Logging;

namespace Sailfish.Mediation;

/// <summary>
///     Sailfish's in-house mediator — the single implementation behind <see cref="IPublisher" /> and
///     <see cref="ISender" />. It replaces the third-party MediatR dependency with the small slice Sailfish
///     actually uses: publish-to-many for notifications and send-to-one for requests.
/// </summary>
/// <remarks>
///     Handlers are resolved from the DI container by the message's <em>runtime</em> type (so a notification
///     published through a base-typed variable still reaches the handlers registered for its concrete type),
///     using a per-type wrapper cached across the process — mirroring MediatR's dispatch semantics. The
///     notification path isolates each handler: a throwing handler is caught and logged, its siblings still
///     run, and the collected failures are rethrown as one <see cref="AggregateException" /> after the
///     fan-out completes. The request path forwards to the single registered handler and lets its result
///     (and any exception) flow straight back to the caller.
/// </remarks>
internal sealed class Mediator(IServiceProvider serviceProvider, ILogger logger) : IPublisher, ISender
{
    // Wrappers are immutable once built, so a static cache is safe and avoids re-reflecting on every
    // publish/send. Notifications key on the message's runtime type alone; requests key on both the request
    // runtime type and the response type, because one request type can implement IRequest<T> for more than
    // one T (and IRequest<out TResponse> is covariant). Keying requests on type alone would hand a cached
    // RequestHandlerWrapper<T1> back for an IRequest<T2> send and throw InvalidCastException on the cast.
    private static readonly Dictionary<Type, NotificationHandlerWrapper> NotificationWrappers = new();
    private static readonly Dictionary<RequestWrapperKey, RequestHandlerWrapper> RequestWrappers = new();
    private static readonly object WrapperLock = new();

    // Composite cache key for request wrappers: a record struct (not a value tuple) so its element names
    // survive when it flows through GetOrAddWrapper's generic TKey parameter.
    private readonly record struct RequestWrapperKey(Type RequestType, Type ResponseType);

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        if (notification is null) throw new ArgumentNullException(nameof(notification));

        var wrapper = GetOrAddWrapper(
            NotificationWrappers,
            notification.GetType(),
            static t => (NotificationHandlerWrapper)Activator.CreateInstance(
                typeof(NotificationHandlerWrapperImpl<>).MakeGenericType(t))!);

        return wrapper.Handle(notification, serviceProvider, logger, cancellationToken);
    }

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var wrapper = (RequestHandlerWrapper<TResponse>)GetOrAddWrapper(
            RequestWrappers,
            new RequestWrapperKey(request.GetType(), typeof(TResponse)),
            static key => (RequestHandlerWrapper)Activator.CreateInstance(
                typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(key.RequestType, key.ResponseType))!);

        return wrapper.Handle(request, serviceProvider, cancellationToken);
    }

    private static TWrapper GetOrAddWrapper<TKey, TWrapper>(Dictionary<TKey, TWrapper> cache, TKey key, Func<TKey, TWrapper> factory)
        where TKey : notnull
    {
        lock (WrapperLock)
        {
            if (cache.TryGetValue(key, out var existing)) return existing;
            var created = factory(key);
            cache[key] = created;
            return created;
        }
    }

    private abstract class NotificationHandlerWrapper
    {
        public abstract Task Handle(object notification, IServiceProvider serviceProvider, ILogger logger, CancellationToken cancellationToken);
    }

    private sealed class NotificationHandlerWrapperImpl<TNotification> : NotificationHandlerWrapper
        where TNotification : INotification
    {
        public override async Task Handle(object notification, IServiceProvider serviceProvider, ILogger logger, CancellationToken cancellationToken)
        {
            var typed = (TNotification)notification;
            List<Exception>? failures = null;

            foreach (var handler in serviceProvider.GetServices<INotificationHandler<TNotification>>())
            {
                try
                {
                    await handler.Handle(typed, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Cancellation is a control-flow signal, not a handler failure: stop the fan-out and propagate.
                    throw;
                }
                catch (Exception ex)
                {
                    (failures ??= []).Add(ex);
                    logger.Log(
                        LogLevel.Error,
                        ex,
                        "Notification handler '{Handler}' threw while handling '{Notification}'. The remaining handlers still run; this failure is aggregated and surfaced after the fan-out completes.",
                        handler.GetType().Name,
                        typeof(TNotification).Name);
                }
            }

            if (failures is { Count: > 0 })
                throw new AggregateException($"One or more handlers for '{typeof(TNotification).Name}' failed.", failures);
        }
    }

    private abstract class RequestHandlerWrapper;

    private abstract class RequestHandlerWrapper<TResponse> : RequestHandlerWrapper
    {
        public abstract Task<TResponse> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
    }

    private sealed class RequestHandlerWrapperImpl<TRequest, TResponse> : RequestHandlerWrapper<TResponse>
        where TRequest : IRequest<TResponse>
    {
        public override Task<TResponse> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var handler = serviceProvider.GetService<IRequestHandler<TRequest, TResponse>>()
                          ?? throw new InvalidOperationException(
                              $"No handler is registered for request '{typeof(TRequest).Name}'. " +
                              $"Register an IRequestHandler<{typeof(TRequest).Name}, {typeof(TResponse).Name}>.");

            return handler.Handle((TRequest)request, cancellationToken);
        }
    }
}
