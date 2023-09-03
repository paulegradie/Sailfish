using System;
using Autofac;
using Demo.API;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PerformanceTests.DemoUtils;

// Example of what can be implemented
public class SailfishDependencies : IDisposable
{
    public IContainer Container { get; set; }

    // single parameterless ctor is all that is allowed
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