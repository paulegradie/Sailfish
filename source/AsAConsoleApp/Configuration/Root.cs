using Autofac;
using Serilog;

namespace AsAConsoleApp.Configuration;

public static class ContainerConfiguration
{
    public static IContainer CompositionRoot()
    {
        var builder = new ContainerBuilder();
        var logger = Logging.CreateLogger("ConsoleAppLogs.log");
        builder.Register<ILogger>(c => logger);
        return builder.Build();
    }
}