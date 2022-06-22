using Autofac;
using Microsoft.AspNetCore.Mvc.Testing;
using Test.API;
using Sailfish.Registration;

namespace AsAConsoleApp
{
    public static class ContainerConfiguration
    {
        public static IContainer CompositionRoot()
        {
            var builder = new ContainerBuilder();
            builder = CustomizeContainer(builder);

            builder.RegisterSailfishTypes();
            builder.RegisterPerformanceTypes(typeof(DemoPerfTest));
            return builder.Build();
        }

        private static ContainerBuilder CustomizeContainer(ContainerBuilder builder)
        {
            builder.RegisterType<WebApplicationFactory<DemoApp>>();
            return builder;
        }
    }
}