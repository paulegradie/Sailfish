using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Demo.API;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using PerformanceTestingConsoleApp.CustomHandlerOverrideExamples;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Registration;
using Serilog;

namespace PerformanceTestingConsoleApp;

public class RegistrationProvider : IProvideARegistrationCallback
{
    public async Task RegisterAsync(ContainerBuilder builder, CancellationToken cancellationToken)
    {
        builder.RegisterType<WebApplicationFactory<DemoApp>>();
        builder.RegisterInstance(Log.Logger).As<ILogger>();
        builder.RegisterType<CustomWriteToCloudHandler>().As<INotificationHandler<WriteCurrentTrackingFileNotification>>();
        builder.RegisterType<CustomNotificationHandler>().As<INotificationHandler<NotifyOnTestResultNotification>>();
        builder.RegisterType<CloudWriter>().As<ICloudWriter>();

        await Task.Yield();
    }
}