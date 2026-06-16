using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Mediation;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Logging;
using Sailfish.Presentation;
using Sailfish.TestAdapter.Display.TestOutputWindow;

namespace Sailfish.TestAdapter.Execution;

public interface ITestAdapterExecutionProgram
{
    Task Run(List<TestCase> testCases, CancellationToken cancellationToken);
}

internal class TestAdapterExecutionProgram : ITestAdapterExecutionProgram
{
    private readonly ILogger _logger;
    private readonly IPublisher _publisher;
    private readonly IRunSettings _runSettings;
    private readonly ISailDiffInternal _sailDiff;
    private readonly IScaleFishInternal _scaleFish;
    private readonly ITestAdapterExecutionEngine _testAdapterExecutionEngine;
    private readonly ITestCaseCountPrinter _testCaseCountPrinter;

    public TestAdapterExecutionProgram(
        IRunSettings runSettings,
        ITestAdapterExecutionEngine testAdapterExecutionEngine,
        IPublisher publisher,
        ISailDiffInternal sailDiff,
        IScaleFishInternal scaleFish,
        ILogger logger,
        ITestCaseCountPrinter testCaseCountPrinter)
    {
        _runSettings = runSettings;
        _testAdapterExecutionEngine = testAdapterExecutionEngine;
        _publisher = publisher;
        _sailDiff = sailDiff;
        _scaleFish = scaleFish;
        _logger = logger;
        _testCaseCountPrinter = testCaseCountPrinter;
    }

    public async Task Run(List<TestCase> testCases, CancellationToken cancellationToken)
    {
        if (testCases.Count == 0)
        {
            _logger.Log(LogLevel.Information, "No Sailfish tests were discovered");
            return;
        }

        _testCaseCountPrinter.SetTestCaseTotal(testCases.Count);
        _testCaseCountPrinter.PrintDiscoveredTotal();

        var executionSummaries = await _testAdapterExecutionEngine.Execute(testCases, cancellationToken);

        // Benchmarks are measured at this point. From here the adapter mirrors the programmatic
        // SailfishExecutor.Run: publish the run-completed notification, then run SailDiff and ScaleFish.
        // Those analyzers publish the SailDiff/ScaleFish completion notifications the Skipper AI handlers
        // listen for, so AI analysis fires on the IDE / `dotnet test` path exactly as it does programmatically.
        // Each post-measurement stage runs inside an error boundary so a failure in analysis never loses the
        // collected timings or crashes the test host.
        await RunPostMeasurementStage(
            "publish test-run-completed notification",
            () => _publisher.Publish(new TestRunCompletedNotification(executionSummaries.ToTrackingFormat()), cancellationToken));

        if (_runSettings.RunSailDiff)
            await RunPostMeasurementStage("SailDiff analysis", () => _sailDiff.Analyze(cancellationToken));

        if (_runSettings.RunScaleFish)
            await RunPostMeasurementStage("ScaleFish analysis", () => _scaleFish.Analyze(executionSummaries, cancellationToken));
    }

    /// <summary>
    ///     Runs a single post-measurement stage (notification publish or an analyzer) inside an error boundary.
    ///     A throw is logged as a structured error so the stage fails soft — the collected timings survive and
    ///     the remaining stages still run. Cancellation is a control-flow signal rather than an analysis failure,
    ///     so it is allowed to propagate. Mirrors <c>SailfishExecutor.RunPostMeasurementStage</c>.
    /// </summary>
    private async Task RunPostMeasurementStage(string stageName, Func<Task> stage)
    {
        try
        {
            await stage().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Log(
                LogLevel.Error,
                ex,
                "Post-measurement stage '{Stage}' failed after benchmarks were measured. The collected timings are preserved and the remaining stages continue; this step's artifacts/analysis were skipped.",
                stageName);
        }
    }
}
