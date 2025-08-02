using Autofac;
using Demo.API;
using Microsoft.AspNetCore.Mvc.Testing;
using System;

namespace Tests.E2E.TestSuite.Utils;

// Example of what they can implement
public class SailfishDependencies : IDisposable
{
    // single parameterless ctor is all this is allowed
    public SailfishDependencies()
    {
        var builder = new ContainerBuilder();
        RegisterThings(builder);
        Container = builder.Build();
    }

    private IContainer Container { get; }

    public void Dispose()
    {
        Container.Dispose();
    }

    private static void RegisterThings(ContainerBuilder builder)
    {
        builder.RegisterType<ExampleDep>().AsSelf();
        builder.RegisterType<SailfishDependencies>().AsSelf();
        builder.RegisterType<WebApplicationFactory<DemoApp>>();
    }

    public T ResolveType<T>() where T : notnull
    {
        return Container.Resolve<T>();
    }
}