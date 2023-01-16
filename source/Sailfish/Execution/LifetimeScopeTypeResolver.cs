using System;
using System.Linq;
using System.Reflection;
using Autofac;
using Sailfish.Attributes;
using Sailfish.Exceptions;
using Sailfish.ExtensionMethods;

namespace Sailfish.Execution;

internal class LifetimeScopeTypeResolver : ITypeResolver
{
    private readonly ILifetimeScope lifetimeScope;

    public LifetimeScopeTypeResolver(ILifetimeScope lifetimeScope)
    {
        this.lifetimeScope = lifetimeScope;
    }

    public object ResolveType(Type type)
    {
        var resolvedType = lifetimeScope?.Resolve(type);
        if (resolvedType is not null) return resolvedType;
        var message = $"Please register {type.Name} using the `registerAdditionalTypes` callback in "
                      + $"the main Run method.\r\nSee the Sailfish demo console app for an example.";
        throw new SailfishException(message);

    }
}
