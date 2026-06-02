using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Sailfish.Contracts.Public.Models;
using Sailfish.Exceptions;
using Sailfish.Extensions.Methods;

namespace Sailfish.Execution;

public interface ITypeActivator
{
    TestInstanceActivation CreateDehydratedTestInstance(Type test, TestCaseId testCaseId, bool disabled = false);
}

public class TypeActivator : ITypeActivator
{
    private readonly IServiceProvider _services;

    public TypeActivator(IServiceProvider services)
    {
        _services = services;
    }

    public TestInstanceActivation CreateDehydratedTestInstance(Type test, TestCaseId testCaseId, bool disabled)
    {
        var ctorArgTypes = test.GetCtorParamTypes();
        if (disabled)
        {
            // Disabled tests are never executed, so they are not resolved through the container (no scope).
            try
            {
                return new TestInstanceActivation(
                    Activator.CreateInstance(test, ctorArgTypes.Select(_ => null! as object).ToArray())!,
                    null);
            }
            catch (Exception ex)
            {
                throw new TestClassInstantiationException(test, ex);
            }
        }

        if (ctorArgTypes.Length != ctorArgTypes.Distinct().Count()) throw new SailfishException($"Multiple ctor arguments of the same type were found in {test.Name}");

        // Resolve this test case from its own DI scope. Scoped/transient dependencies are fresh per case and are
        // disposed when the scope is disposed; singletons (a shared server, an ISailfishFixture<T>, anything
        // registered AddSingleton) come from the root container and are shared across every case. The engine owns
        // and disposes the returned scope after the case completes.
        var scope = _services.CreateScope();
        try
        {
            var provider = scope.ServiceProvider;

            List<object> ctorArgs;
            try
            {
                ctorArgs = ctorArgTypes
                    .Where(x => x != typeof(TestCaseId))
                    .Select(x => provider.GetService(x)
                        ?? throw new SailfishException(
                            $"No registration found for type '{x.FullName}' required by test class '{test.FullName}'. " +
                            "Register it via IServiceCollection — typically by implementing IRegisterSailfishServices " +
                            "in your test assembly."))
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

            return new TestInstanceActivation(obj, scope);
        }
        catch
        {
            // Never leak the per-case scope if activation failed for any reason.
            scope.Dispose();
            throw;
        }
    }
}
