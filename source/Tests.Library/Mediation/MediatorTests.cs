using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Sailfish.Logging;
using Sailfish.Mediation;
using Shouldly;
using Xunit;

namespace Tests.Library.Mediation;

/// <summary>
/// Direct tests for the in-house <see cref="Mediator"/> (the replacement for MediatR). These exercise the
/// real dispatcher rather than a mock, so they lock in the behaviors the structural review asked for:
/// publish fans out to every handler, a throwing handler is isolated from its siblings and surfaced as an
/// aggregate (fail-loud), cancellation propagates as-is, and send routes to exactly one handler.
/// </summary>
public class MediatorTests
{
    private static ServiceProvider BuildProvider(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<ILogger>());
        services.AddTransient<IPublisher, Mediator>();
        services.AddTransient<ISender, Mediator>();
        configure(services);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Publish_InvokesAllRegisteredHandlers_InRegistrationOrder()
    {
        var provider = BuildProvider(s =>
        {
            s.AddTransient<INotificationHandler<PingNotification>, FirstHandler>();
            s.AddTransient<INotificationHandler<PingNotification>, SecondHandler>();
        });
        var notification = new PingNotification();

        await provider.GetRequiredService<IPublisher>().Publish(notification);

        notification.Log.ShouldBe(new[] { "first", "second" });
    }

    [Fact]
    public async Task Publish_WithNoRegisteredHandlers_IsANoOp()
    {
        var provider = BuildProvider(_ => { });

        await Should.NotThrowAsync(() => provider.GetRequiredService<IPublisher>().Publish(new PingNotification()));
    }

    [Fact]
    public async Task Publish_WhenOneHandlerThrows_RunsRemainingHandlers_ThenThrowsAggregate()
    {
        var provider = BuildProvider(s =>
        {
            s.AddTransient<INotificationHandler<PingNotification>, ThrowingHandler>();
            s.AddTransient<INotificationHandler<PingNotification>, SecondHandler>();
        });
        var notification = new PingNotification();

        var aggregate = await Should.ThrowAsync<AggregateException>(
            () => provider.GetRequiredService<IPublisher>().Publish(notification));

        // The throwing handler did not abort its sibling — both ran.
        notification.Log.ShouldContain("threw");
        notification.Log.ShouldContain("second");
        aggregate.InnerExceptions.ShouldContain(e => e is InvalidOperationException);
    }

    [Fact]
    public async Task Publish_WhenHandlerCancels_PropagatesOperationCanceled_NotAggregate()
    {
        var provider = BuildProvider(s => s.AddTransient<INotificationHandler<PingNotification>, CancellingHandler>());

        await Should.ThrowAsync<OperationCanceledException>(
            () => provider.GetRequiredService<IPublisher>().Publish(new PingNotification()));
    }

    [Fact]
    public async Task Publish_ResolvesHandlersByRuntimeType_EvenWhenPublishedAsBaseInterface()
    {
        var provider = BuildProvider(s => s.AddTransient<INotificationHandler<PingNotification>, FirstHandler>());
        INotification asBase = new PingNotification();

        await provider.GetRequiredService<IPublisher>().Publish(asBase);

        ((PingNotification)asBase).Log.ShouldContain("first");
    }

    [Fact]
    public async Task Send_RoutesToTheSingleHandler_AndReturnsItsResponse()
    {
        var provider = BuildProvider(s => s.AddTransient<IRequestHandler<AddRequest, int>, AddHandler>());

        var result = await provider.GetRequiredService<ISender>().Send(new AddRequest(2, 3));

        result.ShouldBe(5);
    }

    [Fact]
    public async Task Send_WithNoRegisteredHandler_Throws()
    {
        var provider = BuildProvider(_ => { });

        await Should.ThrowAsync<InvalidOperationException>(
            () => provider.GetRequiredService<ISender>().Send(new AddRequest(1, 1)));
    }

    [Fact]
    public async Task Send_SameRequestType_DifferentResponseTypes_RoutesEachToItsOwnHandler()
    {
        // Regression: the wrapper cache must key on (request type, response type), not request type alone.
        // One request type can implement IRequest<T> for more than one T (and IRequest<out TResponse> is
        // covariant). Keying on request type alone hands the first send's RequestHandlerWrapper<int> back to
        // the second send and throws InvalidCastException when casting it to RequestHandlerWrapper<string>.
        var provider = BuildProvider(s =>
        {
            s.AddTransient<IRequestHandler<MultiResponseRequest, int>, MultiIntHandler>();
            s.AddTransient<IRequestHandler<MultiResponseRequest, string>, MultiStringHandler>();
        });
        var sender = provider.GetRequiredService<ISender>();
        var request = new MultiResponseRequest();

        var asInt = await sender.Send<int>(request);
        var asString = await sender.Send<string>(request);

        asInt.ShouldBe(42);
        asString.ShouldBe("forty-two");
    }

    [Fact]
    public async Task Send_WhenMultipleHandlersRegistered_LastRegistrationWins()
    {
        // This is the documented override mechanism for requests: a consumer registers their handler after
        // the framework default, and it wins.
        var provider = BuildProvider(s =>
        {
            s.AddTransient<IRequestHandler<AddRequest, int>, AddHandler>();
            s.AddTransient<IRequestHandler<AddRequest, int>, OverridingAddHandler>();
        });

        var result = await provider.GetRequiredService<ISender>().Send(new AddRequest(1, 1));

        result.ShouldBe(999);
    }

    public sealed class PingNotification : INotification
    {
        public List<string> Log { get; } = [];
    }

    public sealed class FirstHandler : INotificationHandler<PingNotification>
    {
        public Task Handle(PingNotification notification, CancellationToken cancellationToken)
        {
            notification.Log.Add("first");
            return Task.CompletedTask;
        }
    }

    public sealed class SecondHandler : INotificationHandler<PingNotification>
    {
        public Task Handle(PingNotification notification, CancellationToken cancellationToken)
        {
            notification.Log.Add("second");
            return Task.CompletedTask;
        }
    }

    public sealed class ThrowingHandler : INotificationHandler<PingNotification>
    {
        public Task Handle(PingNotification notification, CancellationToken cancellationToken)
        {
            notification.Log.Add("threw");
            throw new InvalidOperationException("boom");
        }
    }

    public sealed class CancellingHandler : INotificationHandler<PingNotification>
    {
        public Task Handle(PingNotification notification, CancellationToken cancellationToken)
            => throw new OperationCanceledException();
    }

    public sealed class AddRequest(int a, int b) : IRequest<int>
    {
        public int A { get; } = a;
        public int B { get; } = b;
    }

    public sealed class AddHandler : IRequestHandler<AddRequest, int>
    {
        public Task<int> Handle(AddRequest request, CancellationToken cancellationToken)
            => Task.FromResult(request.A + request.B);
    }

    public sealed class OverridingAddHandler : IRequestHandler<AddRequest, int>
    {
        public Task<int> Handle(AddRequest request, CancellationToken cancellationToken)
            => Task.FromResult(999);
    }

    // A single request type that produces two different response types — exercises the (request, response)
    // wrapper-cache key.
    public sealed class MultiResponseRequest : IRequest<int>, IRequest<string>;

    public sealed class MultiIntHandler : IRequestHandler<MultiResponseRequest, int>
    {
        public Task<int> Handle(MultiResponseRequest request, CancellationToken cancellationToken)
            => Task.FromResult(42);
    }

    public sealed class MultiStringHandler : IRequestHandler<MultiResponseRequest, string>
    {
        public Task<string> Handle(MultiResponseRequest request, CancellationToken cancellationToken)
            => Task.FromResult("forty-two");
    }
}
