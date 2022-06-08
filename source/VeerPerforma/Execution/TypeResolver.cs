using Autofac;

namespace VeerPerforma.Execution;

public class TypeResolver : ITypeResolver
{
    private readonly IServiceProvider? serviceProvider;
    private readonly ILifetimeScope? lifetimeScope;

    public TypeResolver(ILifetimeScope lifetimeScope)
    {
        this.lifetimeScope = lifetimeScope;
    }

    public TypeResolver(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public object ResolveType(Type type)
    {
        if (lifetimeScope is not null)
            return lifetimeScope.Resolve(type);
        else if (serviceProvider is not null)
        {
            var resolved = serviceProvider.GetService(type);
            if (resolved is null) throw new Exception($"Could not resolve test type: {type.Name}. Make sure you register this type in a autofac or service collection container.");
            return resolved;
        }
        else
            throw new Exception("Service provider not found from whence to resolve objects");
    }
}