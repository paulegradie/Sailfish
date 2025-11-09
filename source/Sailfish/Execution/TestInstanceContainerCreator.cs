using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sailfish.Attributes;
using Sailfish.Contracts.Public.Models;
using Sailfish.Extensions.Methods;

namespace Sailfish.Execution;

internal interface ITestInstanceContainerCreator
{
    List<TestInstanceContainerProvider> CreateTestContainerInstanceProviders(
        Type test,
        Func<PropertySet, bool>? propertyTensorFilter = null,
        Func<MethodInfo, bool>? instanceContainerFilter = null);
}

internal class TestInstanceContainerCreator(
    IRunSettings runSettings,
    ITypeActivator typeActivator,
    IPropertySetGenerator propertySetGenerator) : ITestInstanceContainerCreator
{
    private readonly IPropertySetGenerator propertySetGenerator = propertySetGenerator;
    private readonly IRunSettings runSettings = runSettings;
    private readonly ITypeActivator typeActivator = typeActivator;

    public List<TestInstanceContainerProvider> CreateTestContainerInstanceProviders(
        Type testType,
        Func<PropertySet, bool>? propertyTensorFilter = null,
        Func<MethodInfo, bool>? instanceContainerFilter = null)
    {
        var variableSets = propertySetGenerator.GenerateSailfishVariableSets(testType, out _);
        if (propertyTensorFilter is not null) variableSets = variableSets.Where(propertyTensorFilter);

        // Determine optional randomization seed (global) for reproducible ordering
        var seed = TryParseSeed(runSettings.Args);

        // Randomize property set order if a seed is provided
        var variableSetsList = variableSets.ToList();
        if (seed.HasValue)
        {
            var rngForProps = new Random(Combine(seed.Value, testType.FullName));
            variableSetsList = variableSetsList.OrderBy(_ => rngForProps.Next()).ToList();
        }

        // Preserve explicit method Order; randomize only among unordered methods when seeded
        var allMethods = testType.GetMethodsWithAttribute<SailfishMethodAttribute>().ToList();
        var orderedMethods = allMethods
            .Select(m => new { Method = m, Attr = m.GetCustomAttribute<SailfishMethodAttribute>() })
            .Where(x => (x.Attr?.Order ?? int.MaxValue) < int.MaxValue)
            .OrderBy(x => x.Attr!.Order)
            .Select(x => x.Method)
            .ToList();
        var unorderedMethods = allMethods
            .Where(m => (m.GetCustomAttribute<SailfishMethodAttribute>()?.Order ?? int.MaxValue) == int.MaxValue)
            .ToList();
        if (seed.HasValue)
        {
            var rngForMethods = new Random(Combine(seed.Value, testType.FullName + ":methods"));
            unorderedMethods = unorderedMethods.OrderBy(_ => rngForMethods.Next()).ToList();
        }
        var sailfishMethods = orderedMethods.Concat(unorderedMethods).ToList();

        if (instanceContainerFilter is not null) sailfishMethods = sailfishMethods.Where(instanceContainerFilter).ToList();

        return sailfishMethods
            .Select(instanceContainer => new TestInstanceContainerProvider(
                runSettings,
                typeActivator,
                testType,
                variableSetsList,
                instanceContainer))
            .ToList();
    }

    private static int Combine(int seed, string? s)
    {
        unchecked
        {
            return seed ^ (s?.GetHashCode() ?? 0);
        }
    }

    private static int? TryParseSeed(Sailfish.Extensions.Types.OrderedDictionary args)
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