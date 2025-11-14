using System;
using System.Linq;
using System.Reflection;
using Autofac;
using Sailfish.Contracts.Public.Models;
using Sailfish.Exceptions;
using Sailfish.Extensions.Methods;

namespace Sailfish.Execution;

public interface ITypeActivator
{
    object CreateDehydratedTestInstance(Type test, TestCaseId testCaseId, bool disabled = false);
}

public class TypeActivator(ILifetimeScope lifetimeScope) : ITypeActivator
{
    private readonly ILifetimeScope _lifetimeScope = lifetimeScope;

    public object CreateDehydratedTestInstance(Type test, TestCaseId testCaseId, bool disabled)
    {
        var ctorArgTypes = test.GetCtorParamTypes();
        if (disabled) return Activator.CreateInstance(test, ctorArgTypes.Select(_ => null! as object).ToArray())!;

        if (ctorArgTypes.Length != ctorArgTypes.Distinct().Count()) throw new SailfishException($"Multiple ctor arguments of the same type were found in {test.Name}");

        var ctorArgs = ctorArgTypes.Where(x => x != typeof(TestCaseId)).Select(x => _lifetimeScope.Resolve(x)).ToList();
        if (ctorArgTypes.Contains(typeof(TestCaseId)))
        {
            var caseIdIndex = Array.IndexOf(ctorArgTypes, typeof(TestCaseId));
            ctorArgs.Insert(caseIdIndex, testCaseId);
        }

        var constructors = test.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        var obj = constructors.Length switch
        {
            0 => Activator.CreateInstance(test),
            1 => constructors.Single().Invoke([.. ctorArgs]),
            _ => throw new SailfishException("Sailfish tests are allowed only a single constructor")
        };

        if (obj is null) throw new SailfishException($"Couldn't create instance of {test.Name}");

        return obj;
    }
}