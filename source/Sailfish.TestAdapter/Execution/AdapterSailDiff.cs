using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Analysis.SailDiff;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Requests;
using Sailfish.Execution;
using Sailfish.Logging;

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
    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly IRunSettings _runSettings;
    private readonly ISailDiffConsoleWindowMessageFormatter _sailDiffConsoleWindowMessageFormatter;
    private readonly IStatisticalTestComputer _statisticalTestComputer;

    public AdapterSailDiff(
        IMediator mediator,
        IRunSettings runSettings,
        ISailDiffConsoleWindowMessageFormatter sailDiffConsoleWindowMessageFormatter,
        IStatisticalTestComputer statisticalTestComputer,
        ILogger logger)
    {
        _mediator = mediator;
        _runSettings = runSettings;
        _sailDiffConsoleWindowMessageFormatter = sailDiffConsoleWindowMessageFormatter;
        _statisticalTestComputer = statisticalTestComputer;
        _logger = logger;
    }

    public async Task Analyze(CancellationToken cancellationToken)
    {
        if (!_runSettings.RunSailDiff) return;

        var beforeAndAfterFileLocations = await _mediator
            .Send(new BeforeAndAfterFileLocationRequest(_runSettings.ProvidedBeforeTrackingFiles), cancellationToken)
            .ConfigureAwait(false);

        var beforeAndAfterData = await _mediator
            .Send(
                new ReadInBeforeAndAfterDataRequest(beforeAndAfterFileLocations.BeforeFilePaths,
                    beforeAndAfterFileLocations.AfterFilePaths), cancellationToken)
            .ConfigureAwait(false);

        if (beforeAndAfterData.BeforeData is null || beforeAndAfterData.AfterData is null)
        {
            _logger.Log(LogLevel.Information, "Failed to retrieve tracking data... aborting the test operation");
            return;
        }

        var testResults = _statisticalTestComputer.ComputeTest(
            beforeAndAfterData.BeforeData,
            beforeAndAfterData.AfterData,
            _runSettings.SailDiffSettings);

        if (testResults.Count == 0)
        {
            _logger.Log(LogLevel.Information, "No prior test results found for the current set");
            return;
        }

        var testIds = new TestIds(beforeAndAfterData.BeforeData.TestIds, beforeAndAfterData.AfterData.TestIds);
        var resultsAsMarkdown = _sailDiffConsoleWindowMessageFormatter.FormConsoleWindowMessageForSailDiff(testResults, testIds, _runSettings.SailDiffSettings, cancellationToken);

        await _mediator
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

        var testResults = _statisticalTestComputer.ComputeTest(beforeTestData, afterTestData, _runSettings.SailDiffSettings);

        return new TestCaseSailDiffResult(testResults, new TestIds(beforeIds, afterIds), _runSettings.SailDiffSettings);
    }
}

internal record TestCaseSailDiffResult(
    List<SailDiffResult> SailDiffResults,
    TestIds TestIds,
    SailDiffSettings TestSettings);