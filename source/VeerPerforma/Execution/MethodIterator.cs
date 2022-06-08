using System.Diagnostics;
using Serilog;

namespace VeerPerforma.Execution;

public class MethodIterator : IMethodIterator
{
    private readonly ILogger logger;

    public MethodIterator(ILogger logger)
    {
        this.logger = logger;
    }

    private async Task WarmupIterations(AncillaryInvocation invoker, int numWarmupIterations)
    {
        for (var i = 0; i < numWarmupIterations; i++)
        {
            await invoker.IterationSetup();
            await invoker.ExecutionMethod();
            await invoker.IterationTearDown();
        }
    }

    private async Task MainExecution(AncillaryInvocation invoker, List<double> elapsedMilliseconds)
    {
        var stopwatch = new Stopwatch();
        
        stopwatch.Start();
        await invoker.ExecutionMethod();
        stopwatch.Stop();

        logger.Debug($"Elapsed Ms: {stopwatch.ElapsedMilliseconds}");
        elapsedMilliseconds.Add(stopwatch.ElapsedMilliseconds);

        stopwatch.Reset();
    }

    public async Task<List<string>> IterateMethodNTimesAsync(AncillaryInvocation invoker, int numIterations, int numWarmupIterations)
    {
        await invoker.MethodSetup();
        await WarmupIterations(invoker, numIterations);

        var elapsedMilliseconds = new List<double>();
        for (var i = 0; i < numIterations; i++)
        {
            await invoker.IterationSetup();

            await MainExecution(invoker, elapsedMilliseconds);

            await invoker.IterationTearDown();
        }

        await invoker.MethodTearDown();
        return new List<string>(); // TODO: provide any messages or delete  
    }
}