using Serilog;
using VeerPerforma.Utils;

namespace VeerPerforma.Execution;

public class TestExecutor : ITestExecutor
{
    private readonly ITestRunPreparation testRunPreparation;
    private readonly ILogger logger;
    private readonly IMethodIterator methodIterator;

    public TestExecutor(
        ILogger logger,
        ITestRunPreparation testRunPreparation,
        IMethodIterator methodIterator
    )
    {
        this.logger = logger;
        this.testRunPreparation = testRunPreparation;
        this.methodIterator = methodIterator;
    }

    public async Task<int> Execute(Type[] tests)
    {
        // TODO: Allow grouping using an IGrouping and Task.WhenAll()
        foreach (var test in tests)
        {
            await Execute(test);
        }

        return await Task.FromResult(0);
    }

    private async Task Execute(Type test)
    {
        var methodMap = testRunPreparation.GenerateTestInstances(test);
        var numIterations = test.GetNumIterations();
        var numWarmupIterations = test.GetWarmupIterations();

        foreach (var methodName in methodMap.Keys.OrderBy(x => x))
        {
            var methodPairs = methodMap[methodName];
            foreach (var pair in methodPairs)
            {
                var (method, instance) = pair;
                var invoker = new AncillaryInvocation(instance, method);

                await invoker.GlobalSetup();
                await methodIterator.IterateMethodNTimesAsync(invoker, numIterations, numWarmupIterations);
                await invoker.GlobalTeardown();
                logger.Debug($"------ Method {method.Name} is finished!");
            }
        }
    }
}