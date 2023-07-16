using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MathNet.Numerics.Statistics;

namespace Sailfish.Execution;

public class OverheadEstimator
{
    private const double NumMilliSecondsToWait = 30.0;
    private static double TicksPerMillisecond => Stopwatch.Frequency / (double)1_000;
    private static double ExpectedWaitPeriodInTicks => TicksPerMillisecond * NumMilliSecondsToWait;

    public int Estimate()
    {
        var method = typeof(OverheadEstimator).GetMethod(nameof(Wait));

        var totalElapsedTicks = new List<double>();

        for (var i = 0; i < 5; i++)
        {
            var stopwatch = Stopwatch.StartNew();

            // Invoke the method using reflection
            method?.Invoke(this, null);

            stopwatch.Stop();
            totalElapsedTicks.Add(stopwatch.ElapsedTicks);
        }

        var averageElapsedTicks = totalElapsedTicks.Mean();
        var overheadInAverageTicks = averageElapsedTicks - ExpectedWaitPeriodInTicks;

        if (overheadInAverageTicks < 0) return 0;

        var estimate = (int)Math.Round(overheadInAverageTicks, 0);
        return estimate;
    }

    public void Wait()
    {
        Thread.Sleep((int)NumMilliSecondsToWait);
    }
}

internal class TestCaseIterator : ITestCaseIterator
{
    public async Task<TestExecutionResult> Iterate(TestInstanceContainer testInstanceContainer, CancellationToken cancellationToken)
    {
        var warmupResult = await WarmupIterations(testInstanceContainer, cancellationToken);
        if (!warmupResult.IsSuccess)
        {
            return warmupResult;
        }

        for (var i = 0; i < testInstanceContainer.NumIterations; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await testInstanceContainer.Invocation.IterationSetup(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return CatchAndReturn(testInstanceContainer, ex);
            }

            cancellationToken.ThrowIfCancellationRequested();

            await testInstanceContainer.Invocation.ExecutionMethod(cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await testInstanceContainer.Invocation.IterationTearDown(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return CatchAndReturn(testInstanceContainer, ex);
            }
        }

        return new TestExecutionResult(testInstanceContainer);
    }

    private static async Task<TestExecutionResult> WarmupIterations(TestInstanceContainer testInstanceContainer, CancellationToken cancellationToken)
    {
        for (var i = 0; i < testInstanceContainer.NumWarmupIterations; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await testInstanceContainer.Invocation.IterationSetup(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return CatchAndReturn(testInstanceContainer, ex);
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await testInstanceContainer.Invocation.ExecutionMethod(cancellationToken, timed: false).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return CatchAndReturn(testInstanceContainer, ex);
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await testInstanceContainer.Invocation.IterationTearDown(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return CatchAndReturn(testInstanceContainer, ex);
            }
        }

        return new TestExecutionResult(testInstanceContainer);
    }

    private static TestExecutionResult CatchAndReturn(TestInstanceContainer testProvider, Exception ex)
    {
        return new TestExecutionResult(testProvider, ex);
    }
}