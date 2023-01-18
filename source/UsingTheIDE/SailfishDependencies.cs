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
}