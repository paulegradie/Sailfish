using Autofac;
using VeerPerforma.Registration;

namespace PerfTestProjectDemo;

public static class ContainerConfiguration
{
    public static IContainer CompositionRoot()
    {
        var builder = new ContainerBuilder();
        builder = CustomizeContainer(builder);

        builder.RegisterVeerPerformaTypes();

        return builder.Build();
    }

    private static ContainerBuilder CustomizeContainer(ContainerBuilder builder)
    {
        return builder;
    }
}