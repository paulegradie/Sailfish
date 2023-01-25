using System;
using System.Linq;
using Autofac;
using MediatR;
using Sailfish.Contracts.Private;
using Sailfish.Contracts.Public.Commands;
using Sailfish.DefaultHandlers;
using Sailfish.Execution;

namespace Sailfish.Registration;

public static class AssemblyRegistrationExtensionMethods
{
    public static void RegisterSailfishTypes(this ContainerBuilder builder)
    {
        builder.RegisterModule(new SailfishModule());
        builder
            .RegisterType<Mediator>()
            .As<IMediator>()
            .InstancePerLifetimeScope();
        builder.Register<ServiceFactory>(
            context =>
            {
                var c = context.Resolve<IComponentContext>();
                return t => c.Resolve(t);
            });

        // builder.RegisterType<SailfishBeforeAndAfterFileLocationHandler>().As<IRequestHandler<BeforeAndAfterFileLocationCommand, BeforeAndAfterFileLocationResponse>>();
        // builder.RegisterType<SailfishReadInBeforeAndAfterDataHandler>().As<IRequestHandler<ReadInBeforeAndAfterDataCommand, ReadInBeforeAndAfterDataResponse>>();
        // builder.RegisterType<SailfishWriteTestResultAsCsvHandler>().As<INotificationHandler<WriteTestResultsAsCsvCommand>>();
        // builder.RegisterType<SailfishWriteTestResultsAsMarkdownHandler>().As<INotificationHandler<WriteTestResultsAsMarkdownCommand>>();
        // builder.RegisterType<WriteToConsoleHandler>().As<INotificationHandler<WriteToConsoleCommand>>();
        // builder.RegisterType<SailfishWriteToMarkdownHandler>().As<INotificationHandler<WriteToMarkDownCommand>>();
        // builder.RegisterType<SailfishWriteTrackingFileHandler>().As<INotificationHandler<WriteCurrentTrackingFileCommand>>();
        // builder.RegisterType<WriteToCsvHandler>().As<INotificationHandler<WriteToCsvCommand>>();

        builder.RegisterAssemblyTypes(typeof(SailfishExecutor).Assembly).Where(x => x != typeof(ISailfishDependency)).AsImplementedInterfaces(); // via assembly scan
    }

    public static void RegisterPerformanceTypes(this ContainerBuilder builder, params Type[] sourceTypes)
    {
        var testCollector = new TestCollector();
        var allPerfTypes = testCollector.CollectTestTypes(sourceTypes);
        builder.RegisterTypes(allPerfTypes);
    }
}