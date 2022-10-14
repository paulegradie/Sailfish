using Autofac;
using Sailfish;
using Serilog;

namespace AsAConsoleApp.Configuration;

public static class ContainerConfiguration
{
    public static IContainer CompositionRoot()
    {
        var builder = new ContainerBuilder();
        var logger = Logging.CreateLogger("ConsoleAppLogs.log");
        builder.Register<ILogger>(c => { return logger; });
        builder.RegisterType<Sailfish.Sailfish>().AsSelf();
        return builder.Build();
    }
}