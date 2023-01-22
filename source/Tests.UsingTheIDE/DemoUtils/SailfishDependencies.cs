using System;
using Autofac;
using Sailfish.AdapterUtils;

namespace Tests.UsingTheIDE.DemoUtils;

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
    }

    public void Dispose()
    {
        Container.Dispose();
    }

    public object ResolveType(Type type)
    {
        return Container.Resolve(type);
    }

    public T ResolveType<T>()
    {
        return Container.Resolve<T>();
    }
}