using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Sailfish.Attributes;
using Sailfish.Contracts.Public.Models;
using Sailfish.Extensions.Methods;
using Sailfish.Utils;

namespace Sailfish.Execution;

internal interface ITestInstanceContainerProvider
{
    public Type Test { get; }
    int GetNumberOfPropertySetsInTheQueue();

    /// <param name="sharedInstance">
    ///     When non-null (SharedInstance lifetime), every yielded case reuses this one class-level instance and
    ///     carries no DI scope of its own (the caller owns the shared instance's scope). When null (PerCase
    ///     lifetime), a fresh instance + per-case scope is created for each case.
    /// </param>
    IEnumerable<TestInstanceContainer> ProvideNextTestCaseEnumeratorForClass(object? sharedInstance = null);
}

internal class TestInstanceContainerProvider : ITestInstanceContainerProvider
{
    private readonly LifecycleMethodTracker? _lifecycleMethodTracker;
    private readonly IEnumerable<PropertySet> _propertySets;
    private readonly IRunSettings _runSettings;
    private readonly ITypeActivator _typeActivator;

    public TestInstanceContainerProvider(
        IRunSettings runSettings,
        ITypeActivator typeActivator,
        Type test,
        IEnumerable<PropertySet> propertySets,
        MethodInfo method,
        LifecycleMethodTracker? lifecycleMethodTracker = null)
    {
        Method = method;
        Test = test;
        _lifecycleMethodTracker = lifecycleMethodTracker;
        _propertySets = propertySets;
        _runSettings = runSettings;
        _typeActivator = typeActivator;
    }

    public MethodInfo Method { get; }

    public Type Test { get; }

    public int GetNumberOfPropertySetsInTheQueue()
    {
        return _propertySets.Count();
    }

    public IEnumerable<TestInstanceContainer> ProvideNextTestCaseEnumeratorForClass(object? sharedInstance = null)
    {
        var disabled = TestIsDisabled(Test, Method);
        if (GetNumberOfPropertySetsInTheQueue() is 0)
        {
            var testCaseId = DisplayNameHelper.CreateTestCaseId(Test, Method.Name, [], []); // a uniq id
            var (instance, scope) = AcquireInstance(sharedInstance, testCaseId, disabled);
            var executionSettings = BuildExecutionSettings(instance);
            yield return TestInstanceContainer.CreateTestInstance(instance, Method, [], [], disabled, executionSettings, _lifecycleMethodTracker, scope);
        }
        else
        {
            foreach (var nextPropertySet in _propertySets)
            {
                var propertyNames = nextPropertySet.GetPropertyNames().ToArray();
                var variableValues = nextPropertySet.GetPropertyValues().ToArray();
                var testCaseId = DisplayNameHelper.CreateTestCaseId(Test, Method.Name, propertyNames, variableValues); // a uniq id

                var (instance, scope) = AcquireInstance(sharedInstance, testCaseId, disabled);
                // Re-inject this case's variable values. In SharedInstance mode this re-hydrates the one shared
                // instance before each case; lazy sequential enumeration guarantees the engine finishes one case
                // before the next is yielded, so re-hydrating here is safe.
                HydrateInstanceTestProperties(instance, nextPropertySet);
                var executionSettings = BuildExecutionSettings(instance);
                yield return TestInstanceContainer.CreateTestInstance(instance, Method, propertyNames, variableValues, disabled, executionSettings, _lifecycleMethodTracker, scope);
            }
        }
    }

    private (object instance, IServiceScope? scope) AcquireInstance(object? sharedInstance, TestCaseId testCaseId, bool disabled)
    {
        // SharedInstance mode: every case reuses the one class-level instance; its DI scope is owned and disposed
        // by the engine, so the container carries no scope of its own.
        if (sharedInstance is not null) return (sharedInstance, null);

        var activation = _typeActivator.CreateDehydratedTestInstance(Test, testCaseId, disabled);
        return (activation.Instance, activation.Scope);
    }

    private IExecutionSettings BuildExecutionSettings(object instance)
    {
        var executionSettings = instance.GetType().RetrieveExecutionTestSettings(
            _runSettings.SampleSizeOverride,
            _runSettings.NumWarmupIterationsOverride,
            _runSettings.GlobalUseAdaptiveSampling,
            _runSettings.GlobalTargetCoefficientOfVariation,
            _runSettings.GlobalMaximumSampleSize,
            _runSettings.GlobalUseConfigurableOutlierDetection,
            _runSettings.GlobalOutlierStrategy,
            _runSettings.GlobalMaxConfidenceIntervalWidth,
            _runSettings.GlobalMinimumSampleSize);
        var seed = TryParseSeed(_runSettings.Args);
        if (seed.HasValue) executionSettings.Seed = seed;
        return executionSettings;
    }

    private bool TestIsDisabled(MemberInfo test, MemberInfo method)
    {
        var typeIsDisabled = test.GetCustomAttributes<SailfishAttribute>().Single().Disabled;

        // A method is either a [SailfishMethod] microbenchmark or a [Trawl] load scenario (SF1022 forbids
        // both), so use SingleOrDefault and consider whichever is present.
        var sailfishMethod = method.GetCustomAttributes<SailfishMethodAttribute>().SingleOrDefault();
        var trawl = method.GetCustomAttributes<TrawlAttribute>().SingleOrDefault();
        var methodIsDisabled = (sailfishMethod?.Disabled ?? false) || (trawl?.Disabled ?? false);

        // Run-wide kill switch: a globally disabled Trawl skips load scenarios but leaves benchmarks alone.
        var trawlGloballyDisabled = trawl is not null && _runSettings.TrawlSettings.Disabled;

        return typeIsDisabled || methodIsDisabled || trawlGloballyDisabled;
    }

    private static void HydrateInstanceTestProperties(object obj, PropertySet propertySet)
    {
        foreach (var variable in propertySet.VariableSet)
        {
            var prop = obj.GetType().GetProperties().Single(x => x.Name == variable.Name);
            prop.SetValue(obj, variable.Value);
        }
    }

    private static int? TryParseSeed(Extensions.Types.OrderedDictionary args)
    {
        try
        {
            foreach (var kv in args)
            {
                var key = kv.Key;
                var value = kv.Value;
                if (string.Equals(key, "seed", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(key, "randomseed", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(key, "rng", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(value, out var s)) return s;
                }
            }
        }
        catch { /* ignore */ }
        return null;
    }
}
