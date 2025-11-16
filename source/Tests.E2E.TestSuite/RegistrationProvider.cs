using Autofac;
using Demo.API;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Logging;
using Sailfish.Registration;
using Tests.E2E.TestSuite.Handlers;

namespace Tests.E2E.TestSuite;

public class E2ETestRegistrationProvider : IProvideARegistrationCallback
{
    public async Task RegisterAsync(ContainerBuilder builder, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        builder.RegisterType<WebApplicationFactory<DemoApp>>();
        builder.RegisterType<TestRunCompletedNotificationHandler>().As<INotificationHandler<TestRunCompletedNotification>>();
        builder.RegisterInstance(Log.Logger).As<ILogger>();
    }
}