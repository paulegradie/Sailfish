using System;
using System.Linq;
using System.Reflection;
using Autofac;
using Sailfish.Analysis;
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

    public object CreateDehydratedTestInstance(Type test, TestCaseId testCaseId)
    {
        var ctorArgTypes = test.GetCtorParamTypes();
        if (ctorArgTypes.Length != ctorArgTypes.Distinct().Count())
        {
            throw new SailfishException($"Multiple ctor arguments of the same type were found in {test.Name}");
        }

        var ctorArgs = ctorArgTypes.Where(x => x != typeof(TestCaseId)).Select(x => lifetimeScope.Resolve(x)).ToList();
        if (ctorArgTypes.Contains(typeof(TestCaseId)))
        {
            var caseIdIndex = Array.IndexOf(ctorArgTypes, typeof(TestCaseId));
            ctorArgs.Insert(caseIdIndex, testCaseId);
        }

        var constructors = test.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        object? obj;
        if (constructors.Length == 0)
        {
            obj = Activator.CreateInstance(test);
        }
        else if (constructors.Length == 1)
        {
            obj = constructors.Single().Invoke(ctorArgs.ToArray());
        }
        else
        {
            throw new SailfishException("Sailfish tests are allowed only a single constructor");
        }
        
        if (obj is null) throw new SailfishException($"Couldn't create instance of {test.Name}");

        return obj;
    }
}