using System;
using Autofac;
using MediatR;
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
        builder.RegisterAssemblyTypes(typeof(SailfishExecutor).Assembly).AsImplementedInterfaces(); // via assembly scan
    }

    public static void RegisterPerformanceTypes(this ContainerBuilder builder, params Type[] sourceTypes)
    {
        var testCollector = new TestCollector();
        var allPerfTypes = testCollector.CollectTestTypes(sourceTypes);
        builder.RegisterTypes(allPerfTypes);
    }
}