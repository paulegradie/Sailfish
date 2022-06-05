using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Autofac;
using VeerPerforma.Attributes;
using VeerPerforma.Attributes.TestHarness;
using VeerPerforma.Utils.Discovery;

namespace VeerPerforma.Executor.Prep;

public class TestExecutor : ITestExecutor
{
    private readonly ITestCollector testCollector;
    private readonly ITestFilter testFilter;
    private readonly ILifetimeScope lifetimeScope;
    private readonly IParameterCombinationMaker parameterCombinationMaker;

    public TestExecutor(
        ITestCollector testCollector,
        ITestFilter testFilter,
        ILifetimeScope lifetimeScope,
        IParameterCombinationMaker parameterCombinationMaker)
    {
        this.testCollector = testCollector;
        this.testFilter = testFilter;
        this.lifetimeScope = lifetimeScope;
        this.parameterCombinationMaker = parameterCombinationMaker;
    }

    public async Task<int> Execute(string[] testNames)
    {
        var perfTests = testCollector.CollectTestTypes();
        var filteredTests = await testFilter.FilterAndValidate(perfTests, testNames);

        // TODO: Allow grouping using an IGrouping and Task.WhenAll()
        foreach (var test in filteredTests)
        {
            await RunPerfTest(test);
        }

        return await Task.FromResult(0);
    }

    public async Task<int> Execute(string[] testNames, params Type[] locationTypes)
    {
        var perfTests = testCollector.CollectTestTypes(locationTypes);
        var filteredTests = await testFilter.FilterAndValidate(perfTests, testNames);

        // TODO: Allow grouping using an IGrouping and Task.WhenAll()
        foreach (var test in filteredTests)
        {
            await RunPerfTest(test);
        }

        return await Task.FromResult(0);
    }

    private static Dictionary<string, int[]> GetParams(Type type)
    {
        var dict = new Dictionary<string, int[]>();
        var propertiesWithAttribute = type.GetPropertiesWithAttribute<IterationVariableAttribute>();
        foreach (var prp in propertiesWithAttribute)
        {
            var variableValues = prp
                .GetCustomAttributes()
                .OfType<IterationVariableAttribute>()
                .Single() // multiple is false, so this shouldn't throw - we validate first to give feedback
                .N
                .ToArray();
            dict.Add(prp.Name, variableValues);
        }

        return dict;
    }

    private (List<string>, IEnumerable<IEnumerable<int>>) GenerateParameterGrid(Type test)
    {
        var variableProperties = GetParams(test);
        var propNames = new List<string>();
        var propValues = new List<List<int>>();
        foreach (var (propertyName, values) in variableProperties)
        {
            propNames.Add(propertyName);
            propValues.Add(values.ToList());
        }

        var combos = parameterCombinationMaker.GetAllPossibleCombos(propValues);
        // Propnames = ["PropA", "PropB"]
        // combos = [[1, 2], [1, 4], [2, 2], [2, 4]
        return (propNames, combos);
    }

    private async Task<int> RunPerfTest(Type test)
    {
        await Task.CompletedTask;
        var (propNames, combos) = GenerateParameterGrid(test);

        var instances = CreateInstances(test, combos, propNames);

        foreach (var instance in instances)
        {
            var stopwatch = new Stopwatch();
            var methods = instance
                .GetType()
                .GetMethodsWithAttribute<ExecutePerformanceCheckAttribute>();
            foreach (var method in methods)
            {
                stopwatch.Start();
                if (IsAsyncMethodInfo(method))
                {
                    var task = (Task)method.Invoke(instance, null)!;
                    await task;
                }
                else
                {
                    method.Invoke(instance, null);
                }

                Console.WriteLine(stopwatch.ElapsedMilliseconds / 1000 / 60);
            }
        }

        return 0;
    }

    private bool IsAsyncMethodInfo(MethodInfo method)
    {
        return method.HasAttribute<AsyncStateMachineAttribute>();
    }

    private static ConstructorInfo GetConstructor(Type type)
    {
        var ctorInfos = type.GetDeclaredConstructors();
        if (ctorInfos.Length == 0 || ctorInfos.Length > 1) throw new Exception("A single ctor must be declared in all test types");
        return ctorInfos.Single();
    }

    private static Type[] GetCtorParamTypes(Type type)
    {
        var ctorInfo = GetConstructor(type);
        var argTypes = ctorInfo.GetParameters().Select(x => x.ParameterType).ToArray();
        return argTypes;
    }

    private object CreateTestInstance(Type test, Type[] ctorArgTypes)
    {
        var ctorArgs = ctorArgTypes.Select(x => lifetimeScope.Resolve(x)).ToArray();
        var instance = Activator.CreateInstance(test, ctorArgs);
        if (instance is null) throw new Exception($"Couldn't create instance of {test.Name}");
        return instance;
    }

    private List<object> CreateInstances(Type test, IEnumerable<IEnumerable<int>> combos, List<string> propNames)
    {
        var instances = new List<object>();
        var ctorArgTypes = GetCtorParamTypes(test);

        foreach (var combo in combos)
        {
            var instance = CreateTestInstance(test, ctorArgTypes);

            foreach (var propMeta in propNames.Zip(combo))
            {
                var prop = instance.GetType().GetProperties().Single(x => x.Name == propMeta.First);
                prop.SetValue(instance, propMeta.Second);
            }

            instances.Add(instance);
        }

        return instances;
    }
}