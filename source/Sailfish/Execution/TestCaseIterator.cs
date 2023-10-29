using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Execution;

internal class TestCaseIterator : ITestCaseIterator
{
    private readonly IRunSettings runSettings;

    public TestCaseIterator(IRunSettings runSettings)
    {
        this.runSettings = runSettings;
    }
    
    public async Task<TestCaseExecutionResult> Iterate(TestInstanceContainer testInstanceContainer, bool disableOverheadEstimation, CancellationToken cancellationToken)
    {
        var overheadEstimator = new OverheadEstimator();
        var warmupResult = await WarmupIterations(testInstanceContainer, cancellationToken);
        if (!warmupResult.IsSuccess)
        {
            return warmupResult;
        }

        if (!disableOverheadEstimation)
        {
            await overheadEstimator.Estimate();
        }

        var iterations = runSettings.SampleSizeOverride is not null ? Math.Max(runSettings.SampleSizeOverride.Value, 1) : testInstanceContainer.SampleSize;
        for (var i = 0; i < iterations; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await testInstanceContainer.CoreInvoker.IterationSetup(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return CatchAndReturn(testInstanceContainer, ex);
            }

            cancellationToken.ThrowIfCancellationRequested();

            await testInstanceContainer.CoreInvoker.ExecutionMethod(cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await testInstanceContainer.CoreInvoker.IterationTearDown(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return CatchAndReturn(testInstanceContainer, ex);
            }
        }

        if (!disableOverheadEstimation)
        {
            await overheadEstimator.Estimate();
            testInstanceContainer.ApplyOverheadEstimates(overheadEstimator.GetAverageEstimate());
        }

        return new TestCaseExecutionResult(testInstanceContainer);
    }

    private static async Task<TestCaseExecutionResult> WarmupIterations(TestInstanceContainer testInstanceContainer, CancellationToken cancellationToken)
    {
        for (var i = 0; i < testInstanceContainer.NumWarmupIterations; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await testInstanceContainer.CoreInvoker.IterationSetup(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return CatchAndReturn(testInstanceContainer, ex);
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await testInstanceContainer.CoreInvoker.ExecutionMethod(cancellationToken, timed: false).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return CatchAndReturn(testInstanceContainer, ex);
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await testInstanceContainer.CoreInvoker.IterationTearDown(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return CatchAndReturn(testInstanceContainer, ex);
            }
        }

        return new TestCaseExecutionResult(testInstanceContainer);
    }

    private static TestCaseExecutionResult CatchAndReturn(TestInstanceContainer testProvider, Exception ex)
    {
        return new TestCaseExecutionResult(testProvider, ex);
    }
}