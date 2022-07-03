using AsAConsoleApp.CustomHandlerOverrideExamples;
using Autofac;
using MediatR;
using Sailfish.Contracts.Public.Commands;

namespace AsAConsoleApp.Configuration;

public class OptionalAdditionalRegistrationsCloud : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<CloudWriter>().As<ICloudWriter>();
        builder.RegisterType<CustomWriteToCloudHandler>().As<INotificationHandler<WriteCurrentTrackingFileCommand>>();
        base.Load(builder);
    }
}

public class OptionalAdditionalRegistrationsNotifications : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<CustomNotificationHandler>().As<INotificationHandler<NotifyOnTestResultCommand>>();
        base.Load(builder);
    }
}
