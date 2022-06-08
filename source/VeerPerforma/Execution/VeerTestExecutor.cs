using Serilog;
using VeerPerforma.Utils;

namespace VeerPerforma.Execution;

public class VeerTestExecutor : IVeerTestExecutor
{
    private readonly ITestRunPreparation testRunPreparation;
    private readonly ILogger logger;
    private readonly IMethodIterator methodIterator;

    public VeerTestExecutor(
        ILogger logger,
        ITestRunPreparation testRunPreparation,
        IMethodIterator methodIterator
    )
    {
        this.logger = logger;
        this.testRunPreparation = testRunPreparation;
        this.methodIterator = methodIterator;
    }

    public async Task<int> Execute(Type[] tests, Action<Type, int, int>? callback = null)
    {
        try
        {
            foreach (var test in tests)
            {
                await Execute(test, callback);
            }
        }
        catch (Exception ex)
        {
            logger.Fatal("The Test runner encountered a fatal error: {0}", ex.Message);
        }

        return await Task.FromResult(0);
    }

    public async Task Execute(Type test, Action<Type, int, int>? callback = null)
    {
        var numIterations = test.GetNumIterations();
        var numWarmupIterations = test.GetWarmupIterations();
        var methodMap = testRunPreparation.GenerateTestInstances(test);

        foreach (var methodName in methodMap.Keys.OrderBy(x => x))
        {
            var methodPairs = methodMap[methodName];

            for (var testCaseIndex = 0; testCaseIndex < methodPairs.Count; testCaseIndex++)
            {
                var (method, instance) = methodPairs[testCaseIndex];
                var invoker = new AncillaryInvocation(instance, method);
                try
                {
                    await invoker.GlobalSetup();
                    await methodIterator.IterateMethodNTimesAsync(invoker, numIterations, numWarmupIterations);
                    await invoker.GlobalTeardown();
                    if (callback is not null)
                        callback(test, testCaseIndex, 1);
                }
                catch (Exception ex)
                {
                    if (callback is not null)
                        callback(test, testCaseIndex, 0);
                }

                logger.Debug($"------ Method {method.Name} is finished!");
            }

            ;
        }
    }
}