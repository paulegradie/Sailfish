using MediatR;
using Sailfish.Analysis.SailDiff;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Requests;
using Sailfish.Execution;
using Sailfish.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.TestAdapter.Execution;

internal interface IAdapterSailDiff : ISailDiffInternal
{
    TestCaseSailDiffResult ComputeTestCaseDiff(
        string[] beforeIds,
        string[] afterIds,
        string currentTestDisplayName,
        IClassExecutionSummary classExecutionSummary,
        PerformanceRunResult preloadedLastRun);
}

internal class AdapterSailDiff : IAdapterSailDiff
{
    private readonly IMediator mediator;
    private readonly IRunSettings runSettings;
    private readonly ISailDiffConsoleWindowMessageFormatter sailDiffConsoleWindowMessageFormatter;
    private readonly ILogger logger;
    private readonly IStatisticalTestComputer statisticalTestComputer;

    public AdapterSailDiff(
        IMediator mediator,
        IRunSettings runSettings,
        ISailDiffConsoleWindowMessageFormatter sailDiffConsoleWindowMessageFormatter,
        IStatisticalTestComputer statisticalTestComputer,
        ILogger logger)
    {
        this.mediator = mediator;
        this.runSettings = runSettings;
        this.sailDiffConsoleWindowMessageFormatter = sailDiffConsoleWindowMessageFormatter;
        this.statisticalTestComputer = statisticalTestComputer;
        this.logger = logger;
    }

    public async Task Analyze(CancellationToken cancellationToken)
    {
        if (!runSettings.RunSailDiff) return;

        var beforeAndAfterFileLocations = await mediator
            .Send(new BeforeAndAfterFileLocationRequest(runSettings.ProvidedBeforeTrackingFiles), cancellationToken)
            .ConfigureAwait(false);

        var beforeAndAfterData = await mediator
            .Send(
                new ReadInBeforeAndAfterDataRequest(beforeAndAfterFileLocations.BeforeFilePaths,
                    beforeAndAfterFileLocations.AfterFilePaths), cancellationToken)
            .ConfigureAwait(false);

        if (beforeAndAfterData.BeforeData is null || beforeAndAfterData.AfterData is null)
        {
            logger.Log(LogLevel.Information, "Failed to retrieve tracking data... aborting the test operation");
            return;
        }

        var testResults = statisticalTestComputer.ComputeTest(
            beforeAndAfterData.BeforeData,
            beforeAndAfterData.AfterData,
            runSettings.SailDiffSettings);

        if (testResults.Count == 0)
        {
            logger.Log(LogLevel.Information, "No prior test results found for the current set");
            return;
        }

        var testIds = new TestIds(beforeAndAfterData.BeforeData.TestIds, beforeAndAfterData.AfterData.TestIds);
        var resultsAsMarkdown = sailDiffConsoleWindowMessageFormatter.FormConsoleWindowMessageForSailDiff(testResults, testIds, runSettings.SailDiffSettings, cancellationToken);

        await mediator
            .Publish(new SailDiffAnalysisCompleteNotification(testResults, resultsAsMarkdown), cancellationToken)
            .ConfigureAwait(false);
    }


    public TestCaseSailDiffResult ComputeTestCaseDiff(
        string[] beforeIds,
        string[] afterIds,
        string currentTestDisplayName,
        IClassExecutionSummary classExecutionSummary,
        PerformanceRunResult preloadedLastRun)
    {
        var beforeTestData = new TestData(
            beforeIds,
            new[] { preloadedLastRun });

        var afterTestData = new TestData(afterIds, classExecutionSummary.CompiledTestCaseResults
            .Select(x => x.PerformanceRunResult!)
            .Where(x => x.DisplayName == currentTestDisplayName));

        var testResults = statisticalTestComputer.ComputeTest(beforeTestData, afterTestData, runSettings.SailDiffSettings);

        return new TestCaseSailDiffResult(testResults, new TestIds(beforeIds, afterIds), runSettings.SailDiffSettings);
    }
}

internal record TestCaseSailDiffResult(
    List<SailDiffResult> SailDiffResults,
    TestIds TestIds,
    SailDiffSettings TestSettings);