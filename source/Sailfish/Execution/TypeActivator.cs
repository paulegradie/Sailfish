using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sailfish.Contracts.Public.Models;
using Sailfish.Exceptions;
using Sailfish.Extensions.Methods;

namespace Sailfish.Execution;

public interface ITypeActivator
{
    object CreateDehydratedTestInstance(Type test, TestCaseId testCaseId, bool disabled = false);
}

public class TypeActivator : ITypeActivator
{
    private readonly IServiceProvider _services;

    public TypeActivator(IServiceProvider services)
    {
        _services = services;
    }

    public object CreateDehydratedTestInstance(Type test, TestCaseId testCaseId, bool disabled)
    {
        var ctorArgTypes = test.GetCtorParamTypes();
        if (disabled)
        {
            try
            {
                return Activator.CreateInstance(test, ctorArgTypes.Select(_ => null! as object).ToArray())!;
            }
            catch (Exception ex)
            {
                throw new TestClassInstantiationException(test, ex);
            }
        }

        if (ctorArgTypes.Length != ctorArgTypes.Distinct().Count()) throw new SailfishException($"Multiple ctor arguments of the same type were found in {test.Name}");

        List<object> ctorArgs;
        try
        {
            ctorArgs = ctorArgTypes
                .Where(x => x != typeof(TestCaseId))
                .Select(x => _services.GetService(x)
                    ?? throw new SailfishException(
                        $"No registration found for type '{x.FullName}' required by test class '{test.FullName}'. " +
                        "Register it via IServiceCollection (e.g. an IRegisterSailfishServices implementation) " +
                        "or, in legacy code, via an [Obsolete] IProvideARegistrationCallback."))
                .ToList();
        }
        catch (Exception ex)
        {
            throw new TestClassInstantiationException(
                test,
                ex,
                $"Failed to resolve constructor dependencies for test class '{test.FullName}'. " +
                "Check that all constructor parameter types are registered with the DI container.");
        }

        if (ctorArgTypes.Contains(typeof(TestCaseId)))
        {
            var caseIdIndex = Array.IndexOf(ctorArgTypes, typeof(TestCaseId));
            ctorArgs.Insert(caseIdIndex, testCaseId);
        }

        var constructors = test.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        object? obj;
        try
        {
            obj = constructors.Length switch
            {
                0 => Activator.CreateInstance(test),
                1 => constructors.Single().Invoke([.. ctorArgs]),
                _ => throw new SailfishException("Sailfish tests are allowed only a single constructor")
            };
        }
        catch (SailfishException)
        {
            throw;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw new TestClassInstantiationException(test, ex.InnerException);
        }
        catch (Exception ex)
        {
            throw new TestClassInstantiationException(test, ex);
        }

        if (obj is null) throw new TestClassInstantiationException(test, new InvalidOperationException($"Couldn't create instance of {test.Name}"));

        return obj;
    }
}
