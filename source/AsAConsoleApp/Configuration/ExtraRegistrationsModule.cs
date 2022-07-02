using AsAConsoleApp.CloudExample;
using Autofac;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Sailfish.Contracts.Public.Commands;
using Test.API;

namespace AsAConsoleApp.Configuration;

public class ExtraRegistrationsModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<WebApplicationFactory<DemoApp>>();
        builder.RegisterType<CloudWriter>().As<ICloudWriter>();
        builder.RegisterType<CustomWriteToCloudHandler>().As<INotificationHandler<WriteCurrentTrackingFileCommand>>();
        base.Load(builder);
    }
}