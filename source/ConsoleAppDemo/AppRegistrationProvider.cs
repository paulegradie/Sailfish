using Autofac;
using Demo.API;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using PerformanceTestingUserInvokedConsoleApp.CustomHandlerOverrideExamples;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Registration;

namespace PerformanceTestingUserInvokedConsoleApp;

public class AppRegistrationProvider : IProvideARegistrationCallback
{
    public async Task RegisterAsync(ContainerBuilder builder, CancellationToken cancellationToken = default)
    {
        builder.RegisterType<WebApplicationFactory<DemoApp>>();
        builder.RegisterType<TestRunCompletedNotificationHandler>().As<INotificationHandler<TestRunCompletedNotification>>();
        builder.RegisterType<CloudWriter>().As<ICloudWriter>();
        await Task.Yield();
    }
}