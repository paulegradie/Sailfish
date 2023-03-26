using System;
using System.Linq;
using Autofac;
using Sailfish.Exceptions;
using Sailfish.Extensions.Methods;

namespace Sailfish.Execution;

public class TypeActivator : ITypeActivator
{
    private readonly ILifetimeScope lifetimeScope;

    public TypeActivator(ILifetimeScope lifetimeScope)
    {
        this.lifetimeScope = lifetimeScope;
    }

    public object CreateDehydratedTestInstance(Type test)
    {
        var ctorArgTypes = test.GetCtorParamTypes();

        var ctorArgs = ctorArgTypes.Select(x => lifetimeScope.Resolve(x)).ToArray();
        var obj = Activator.CreateInstance(test, ctorArgs);
        if (obj is null) throw new SailfishException($"Couldn't create instance of {test.Name}");
        return obj;
    }
}