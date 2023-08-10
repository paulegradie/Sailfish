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

        builder.RegisterAssemblyTypes(typeof(SailfishExecutor).Assembly).Where(x => x != typeof(ISailfishDependency)).AsImplementedInterfaces(); // via assembly scan
    }
}