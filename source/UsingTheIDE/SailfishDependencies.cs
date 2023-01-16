using System;
using Autofac;
using Sailfish.AdapterUtils;

namespace UsingTheIDE;

// Example of what they'll implement
public class SailfishDependencies : ISailfishFixtureDependency
{
    public IContainer Container { get; set; }

    public SailfishDependencies()
    {
        var builder = new ContainerBuilder();
        RegisterThings(builder);
        Container = builder.Build();
    }

    private void RegisterThings(ContainerBuilder builder)
    {
        builder.RegisterType<ExampleDep>().AsSelf();
    }

    public void Dispose()
    {
        Container.Dispose();
    }

    public T ResolveType<T>(T type) where T : class
    {
        var thing = Container.Resolve(typeof(T));
        var typed = (T)thing;
        return typed;
    }
}