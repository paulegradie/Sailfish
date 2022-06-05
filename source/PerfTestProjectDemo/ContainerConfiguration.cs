using Autofac;
using Microsoft.AspNetCore.Mvc.Testing;
using Test.API;
using VeerPerforma.Registration;

namespace PerfTestProjectDemo;

public static class ContainerConfiguration
{
    public static IContainer CompositionRoot()
    {
        var builder = new ContainerBuilder();
        builder = CustomizeContainer(builder);

        builder.RegisterVeerPerformaTypes();
        builder.RegisterPerformanceTypes(typeof(CountToAMillionPerformance));
        return builder.Build();
    }

    private static ContainerBuilder CustomizeContainer(ContainerBuilder builder)
    {
        builder.RegisterType<WebApplicationFactory<MyApp>>();
        return builder;
    }
}