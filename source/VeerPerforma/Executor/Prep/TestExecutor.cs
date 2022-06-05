using System.Diagnostics;
using VeerPerforma.Attributes;
using VeerPerforma.Attributes.TestHarness;
using VeerPerforma.Utils.Discovery;

namespace VeerPerforma.Executor.Prep;

public class TestExecutor : ITestExecutor
{
    private readonly ITestCollector testCollector;
    private readonly ITestFilter testFilter;

    public TestExecutor(ITestCollector testCollector, ITestFilter testFilter)
    {
        this.testCollector = testCollector;
        this.testFilter = testFilter;
    }

    public async Task<int> Execute(string[] testsRequestedByUser)
    {
        var perfTests = testCollector.CollectTestTypes();
        var filteredTests = await testFilter.FilterAndValidate(perfTests, testsRequestedByUser);

        // TODO: Allow grouping using an IGrouping and Task.WhenAll()
        foreach (var test in filteredTests)
        {
            await RunPerfTest(test);
        }

        return await Task.FromResult(0);
    }

    private async Task<int> RunPerfTest(Type test)
    {
        await Task.CompletedTask;
        // Can be any number of properties that have an IterationVariableAttribute
        // Can be multiple execution methods - this is the outermost construct after the class, so we should loop over those to call the invoke.
        // We'll need to build an instance of the class, then for each property, iterate through each of them in a grid, and set them - then call the invoke method.

        // 1. Get all of the properties with the IterationVariableAttribute and create the execution grid
        var properties = test.GetPropertiesWithAttribute<IterationVariableAttribute>().ToList();
        var propertyValues = new List<List<string>>();
        foreach (var propertyInfo in properties)
        {
            var currentPropValues = (int[])propertyInfo.GetValue(test)!;
            if (currentPropValues is null) throw new Exception("All iteration variables must have params");
            propertyValues.Add(currentPropValues.Select(x => x.ToString()).ToList());
        }

        var instances = new List<object>();
        var combos = InstanceConstructor.GetAllPossibleCombos(propertyValues);
        var combinations = combos.Select(c => c.Select(x => int.Parse(x)).ToArray()).ToArray();
        foreach (var combo in combinations)
        {
            var instance = InstanceConstructor.CreateInstance(test, combo);
            instances.Add(instance);
        }

        foreach (var instance in instances)
        {
            var stopwatch = new Stopwatch();
            var methods = instance.GetType().GetMethodsWithAttribute<ExecutePerformanceCheckAttribute>();
            foreach (var method in methods)
            {
                stopwatch.Start();
                var task = (Task)method.Invoke(instance, null)!;
                await task;
                Console.WriteLine(stopwatch.ElapsedMilliseconds / 1000 / 60);
            }
        }

        return 0;
    }
}