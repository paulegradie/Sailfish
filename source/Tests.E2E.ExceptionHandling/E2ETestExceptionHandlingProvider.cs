using Autofac;
using Demo.API;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Registration;
using Serilog;
using Tests.E2E.ExceptionHandling.Handlers;

namespace Tests.E2E.ExceptionHandling;

public class E2ETestExceptionHandlingProvider : IProvideARegistrationCallback
{
    public async Task RegisterAsync(ContainerBuilder builder, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        builder.RegisterType<WebApplicationFactory<DemoApp>>();
        builder.RegisterInstance(Log.Logger).As<ILogger>();
        builder.RegisterType<TestRunCompletedNotificationHandler>().As<INotificationHandler<TestRunCompletedNotification>>();
    }
}