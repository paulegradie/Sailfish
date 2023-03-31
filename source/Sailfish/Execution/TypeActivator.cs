using System;
using System.Linq;
using Accord.Math;
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

        var ctorArgs = ctorArgTypes.Where(x => x != typeof(TestCaseId)).Select(x => lifetimeScope.Resolve(x)).ToList();
        if (ctorArgTypes.Contains(typeof(TestCaseId)))
        {
            var caseIdIndex = ctorArgTypes.IndexOf(typeof(TestCaseId));
            ctorArgs.Insert(caseIdIndex, testCaseId);
        }
        var obj = Activator.CreateInstance(test, ctorArgs.ToArray());
        if (obj is null) throw new SailfishException($"Couldn't create instance of {test.Name}");
        return obj;
    }
}