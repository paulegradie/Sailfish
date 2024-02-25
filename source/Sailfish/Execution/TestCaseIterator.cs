using Sailfish.Contracts.Public.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Logging;

namespace Sailfish.Execution;

internal interface ITestCaseIterator
{
    Task<TestCaseExecutionResult> Iterate(TestInstanceContainer testInstanceContainer, bool DisableOverheadEstimation,
        CancellationToken cancellationToken);
}

internal class TestCaseIterator : ITestCaseIterator
{
    private readonly IRunSettings runSettings;
    private readonly ILogger logger;

    public TestCaseIterator(IRunSettings runSettings, ILogger logger)
    {
        this.logger = logger;
        this.runSettings = runSettings;
    }

    public async Task<TestCaseExecutionResult> Iterate(
        TestInstanceContainer testInstanceContainer,
        bool disableOverheadEstimation,
        CancellationToken cancellationToken)
    {
        var overheadEstimator = new OverheadEstimator();
        var warmupResult = await WarmupIterations(testInstanceContainer, cancellationToken);
        if (!warmupResult.IsSuccess) return warmupResult;

        if (!disableOverheadEstimation) await overheadEstimator.Estimate();

        var iterations = runSettings.SampleSizeOverride is not null
            ? Math.Max(runSettings.SampleSizeOverride.Value, 1)
            : testInstanceContainer.SampleSize;

        testInstanceContainer.CoreInvoker.SetTestCaseStart();
        for (var i = 0; i < iterations; i++)
        {
            logger.Log(LogLevel.Information, "      ---- iteration {CurrentIteration} of {TotalIterations}", i + 1,
                iterations);
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
                testInstanceContainer.CoreInvoker.SetTestCaseStop();
                return CatchAndReturn(testInstanceContainer, ex);
            }
        }

        testInstanceContainer.CoreInvoker.SetTestCaseStop();

        if (disableOverheadEstimation)
        {
            return new TestCaseExecutionResult(testInstanceContainer);
        }

        await overheadEstimator.Estimate();
        testInstanceContainer.ApplyOverheadEstimates(overheadEstimator.GetAverageEstimate());

        return new TestCaseExecutionResult(testInstanceContainer);
    }

    private async Task<TestCaseExecutionResult> WarmupIterations(TestInstanceContainer testInstanceContainer,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < testInstanceContainer.NumWarmupIterations; i++)
        {
            logger.Log(LogLevel.Information, "      ---- warmup iteration {CurrentIteration} of {TotalIterations}",
                i + 1,
                testInstanceContainer.NumWarmupIterations);

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await testInstanceContainer.CoreInvoker.IterationSetup(cancellationToken).ConfigureAwait(false);
                await testInstanceContainer.CoreInvoker.ExecutionMethod(cancellationToken, false).ConfigureAwait(false);
                await testInstanceContainer.CoreInvoker.IterationTearDown(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return CatchAndReturn(testInstanceContainer, ex);
            }
        }

        return new TestCaseExecutionResult(testInstanceContainer);
    }

    private TestCaseExecutionResult CatchAndReturn(TestInstanceContainer testProvider, Exception ex)
    {
        if (ex is NullReferenceException)
        {
            ex = new NullReferenceException(ex.Message + Environment.NewLine + $"Null variable or property encountered in method: {testProvider.ExecutionMethod.Name}");
        }
        
        logger.Log(LogLevel.Error, ex, "An exception occured during test execution");
        return new TestCaseExecutionResult(testProvider, ex);
    }
}