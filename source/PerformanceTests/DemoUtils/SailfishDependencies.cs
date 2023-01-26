using System;
using Autofac;
using Microsoft.AspNetCore.Mvc.Testing;
using Sailfish.AdapterUtils;
using Test.API;

namespace PerformanceTests.DemoUtils;

// Example of what they can implement
public class SailfishDependencies : ISailfishFixtureDependency
{
    private IContainer Container { get; set; }

    // single parameterless ctor is all this is allowed
    public SailfishDependencies()
    {
        var builder = new ContainerBuilder();
        RegisterThings(builder);
        Container = builder.Build();
    }

    private static void RegisterThings(ContainerBuilder builder)
    {
        builder.RegisterType<ExampleDep>().AsSelf();
        builder.RegisterType<SailfishDependencies>().AsSelf();
        builder.RegisterType<WebApplicationFactory<DemoApp>>();
    }

    public void Dispose()
    {
        Container.Dispose();
    }

    public object ResolveType(Type type)
    {
        return Container.Resolve(type);
    }

    public T ResolveType<T>() where T : notnull
    {
        return Container.Resolve<T>();
    }
}