using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sailfish.Attributes;
using Sailfish.Contracts.Public.Models;
using Sailfish.Extensions.Methods;
using Sailfish.Utils;

namespace Sailfish.Execution;

internal interface ITestInstanceContainerProvider
{
    public Type Test { get; }
    int GetNumberOfPropertySetsInTheQueue();
    IEnumerable<TestInstanceContainer> ProvideNextTestCaseEnumeratorForClass();
}

internal class TestInstanceContainerProvider : ITestInstanceContainerProvider
{
    private readonly IEnumerable<PropertySet> _propertySets;
    private readonly IRunSettings _runSettings;
    private readonly ITypeActivator _typeActivator;

    public TestInstanceContainerProvider(
        IRunSettings runSettings,
        ITypeActivator typeActivator,
        Type test,
        IEnumerable<PropertySet> propertySets,
        MethodInfo method)
    {
        Method = method;
        Test = test;
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

    public IEnumerable<TestInstanceContainer> ProvideNextTestCaseEnumeratorForClass()
    {
        var disabled = TestIsDisabled(Test, Method);
        if (GetNumberOfPropertySetsInTheQueue() is 0)
        {
            var testCaseId = DisplayNameHelper.CreateTestCaseId(Test, Method.Name, [], []); // a uniq id
            var instance = _typeActivator.CreateDehydratedTestInstance(Test, testCaseId, disabled);
            var executionSettings = instance.GetType().RetrieveExecutionTestSettings(
                _runSettings.SampleSizeOverride,
                _runSettings.NumWarmupIterationsOverride,
                _runSettings.GlobalUseAdaptiveSampling,
                _runSettings.GlobalTargetCoefficientOfVariation,
                _runSettings.GlobalMaximumSampleSize,
                _runSettings.GlobalUseConfigurableOutlierDetection,
                _runSettings.GlobalOutlierStrategy);
            var seed = TryParseSeed(_runSettings.Args);
            if (seed.HasValue) executionSettings.Seed = seed;
            yield return TestInstanceContainer.CreateTestInstance(instance, Method, [], [], disabled, executionSettings);
        }
        else
        {
            foreach (var nextPropertySet in _propertySets)
            {
                var propertyNames = nextPropertySet.GetPropertyNames().ToArray();
                var variableValues = nextPropertySet.GetPropertyValues().ToArray();
                var testCaseId = DisplayNameHelper.CreateTestCaseId(Test, Method.Name, propertyNames, variableValues); // a uniq id

                var instance = _typeActivator.CreateDehydratedTestInstance(Test, testCaseId, disabled);
                HydrateInstanceTestProperties(instance, nextPropertySet);

                var executionSettings = instance.GetType().RetrieveExecutionTestSettings(
                    _runSettings.SampleSizeOverride,
                    _runSettings.NumWarmupIterationsOverride,
                    _runSettings.GlobalUseAdaptiveSampling,
                    _runSettings.GlobalTargetCoefficientOfVariation,
                    _runSettings.GlobalMaximumSampleSize,
                    _runSettings.GlobalUseConfigurableOutlierDetection,
                    _runSettings.GlobalOutlierStrategy);
                var seed = TryParseSeed(_runSettings.Args);
                if (seed.HasValue) executionSettings.Seed = seed;
                yield return TestInstanceContainer.CreateTestInstance(instance, Method, propertyNames, variableValues, disabled, executionSettings);
            }
        }
    }

    private static bool TestIsDisabled(MemberInfo test, MemberInfo method)
    {
        var typeIsDisabled = test.GetCustomAttributes<SailfishAttribute>().Single().Disabled;
        var methodIsDisabled = method.GetCustomAttributes<SailfishMethodAttribute>().Single().Disabled;
        return methodIsDisabled || typeIsDisabled;
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