using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Requests;
using Sailfish.DefaultHandlers.Sailfish;
using Sailfish.Mediation;
using Shouldly;
using Xunit;

namespace Tests.Library.Mediation;

/// <summary>
/// Validates the in-house mediation wiring (the replacement for MediatR's AddMediatR + assembly scan):
/// AddSailfishMediation registers the publisher/sender and discovers the framework's handlers from the
/// Sailfish assembly so the dispatcher can resolve them at runtime.
/// </summary>
public class MediationRegistrationTests
{
    [Fact]
    public void AddSailfishMediation_RegistersPublisherAndSender()
    {
        var services = new ServiceCollection();
        services.AddSailfishMediation();

        services.Any(d => d.ServiceType == typeof(IPublisher)).ShouldBeTrue();
        services.Any(d => d.ServiceType == typeof(ISender)).ShouldBeTrue();
    }

    [Fact]
    public void AddSailfishMediation_DiscoversFrameworkNotificationHandlers()
    {
        var services = new ServiceCollection();
        services.AddSailfishMediation();

        // The default TestRunCompleted handlers are found by the assembly scan and bound to the closed
        // notification-handler interface — this is how a published notification reaches them at runtime.
        services.Any(d =>
                d.ServiceType == typeof(INotificationHandler<TestRunCompletedNotification>) &&
                d.ImplementationType == typeof(CsvTestRunCompletedHandler))
            .ShouldBeTrue();
    }

    [Fact]
    public void AddSailfishMediation_DiscoversFrameworkRequestHandlers()
    {
        var services = new ServiceCollection();
        services.AddSailfishMediation();

        services.Any(d =>
                d.ServiceType == typeof(IRequestHandler<BeforeAndAfterFileLocationRequest, BeforeAndAfterFileLocationResponse>))
            .ShouldBeTrue();
    }
}
