using Autofac;

namespace Sailfish.Tool.Framework.DIContainer
{
    public static class ContainerConfiguration
    {
        public static IContainer CompositionRoot()
        {
            var builder = new ContainerBuilder();
            builder = CustomizeContainer(builder);
            builder.RegisterType<SailfishExecutor>();
            return builder.Build();
        }

        private static ContainerBuilder CustomizeContainer(ContainerBuilder builder)
        {
            return builder;
        }
    }
}