using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Execution;

internal class TestCaseIterator : ITestCaseIterator
{
    public async Task<TestExecutionResult> Iterate(TestInstanceContainer testInstanceContainer, bool disableOverheadEstimation, CancellationToken cancellationToken)
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

        for (var i = 0; i < testInstanceContainer.SampleSize; i++)
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

        return new TestExecutionResult(testInstanceContainer);
    }

    private static async Task<TestExecutionResult> WarmupIterations(TestInstanceContainer testInstanceContainer, CancellationToken cancellationToken)
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

        return new TestExecutionResult(testInstanceContainer);
    }

    private static TestExecutionResult CatchAndReturn(TestInstanceContainer testProvider, Exception ex)
    {
        return new TestExecutionResult(testProvider, ex);
    }
}