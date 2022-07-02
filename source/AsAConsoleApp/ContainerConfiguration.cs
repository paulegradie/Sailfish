using Autofac;
using Microsoft.AspNetCore.Mvc.Testing;
using Sailfish;
using Test.API;
using Serilog;

namespace AsAConsoleApp
{
    public static class ContainerConfiguration
    {
        public static IContainer CompositionRoot()
        {
            var logger = Logging.CreateLogger("ConsoleAppLogs.log");
            var builder = new ContainerBuilder();
            builder.Register<ILogger>(c => { return logger; });
            builder.RegisterType<WebApplicationFactory<DemoApp>>();
            builder.RegisterType<SailfishExecution>().AsSelf();
            return builder.Build();
        }
    }
}