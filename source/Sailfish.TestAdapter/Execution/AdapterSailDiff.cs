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

namespace Sailfish.TestAdapter.Execution;

internal interface IAdapterSailDiff : ISailDiffInternal
{
    string ComputeTestCaseDiff(
        TestCaseExecutionResult testCaseExecutionResult,
        IClassExecutionSummary classExecutionSummary,
        PerformanceRunResult preloadedLastRun,
        CancellationToken cancellationToken);
}

internal class AdapterSailDiff : IAdapterSailDiff
{
    private readonly IMediator mediator;
    private readonly IRunSettings runSettings;
    private readonly IAdapterConsoleWriter consoleWriter;
    private readonly IStatisticalTestComputer statisticalTestComputer;
    private readonly ISailDiffResultMarkdownConverter sailDiffResultMarkdownConverter;

    public AdapterSailDiff(
        IMediator mediator,
        IRunSettings runSettings,
        IAdapterConsoleWriter consoleWriter,
        IStatisticalTestComputer statisticalTestComputer,
        ISailDiffResultMarkdownConverter sailDiffResultMarkdownConverter)
    {
        this.mediator = mediator;
        this.runSettings = runSettings;
        this.consoleWriter = consoleWriter;
        this.statisticalTestComputer = statisticalTestComputer;
        this.sailDiffResultMarkdownConverter = sailDiffResultMarkdownConverter;
    }

    public async Task Analyze(CancellationToken cancellationToken)
    {
        if (!runSettings.RunSailDiff) return;

        var beforeAndAfterFileLocations = await mediator
            .Send(new BeforeAndAfterFileLocationRequest(runSettings.ProvidedBeforeTrackingFiles), cancellationToken)
            .ConfigureAwait(false);

        var beforeAndAfterData = await mediator
            .Send(new ReadInBeforeAndAfterDataRequest(beforeAndAfterFileLocations.BeforeFilePaths, beforeAndAfterFileLocations.AfterFilePaths), cancellationToken)
            .ConfigureAwait(false);

        if (beforeAndAfterData.BeforeData is null || beforeAndAfterData.AfterData is null)
        {
            consoleWriter.WriteString("Failed to retrieve tracking data... aborting the test operation");
            return;
        }

        var testResults = statisticalTestComputer.ComputeTest(
            beforeAndAfterData.BeforeData,
            beforeAndAfterData.AfterData,
            runSettings.SailDiffSettings);

        if (!testResults.Any())
        {
            consoleWriter.WriteString("No prior test results found for the current set");
            return;
        }

        var testIds = new TestIds(beforeAndAfterData.BeforeData.TestIds, beforeAndAfterData.AfterData.TestIds);
        var resultsAsMarkdown = sailDiffResultMarkdownConverter.ConvertToMarkdownTable(testResults, testIds, cancellationToken);

        consoleWriter.WriteStatTestResultsToConsole(resultsAsMarkdown, testIds, runSettings.SailDiffSettings);
        await mediator.Publish(new SailDiffAnalysisCompleteNotification(testResults, resultsAsMarkdown), cancellationToken).ConfigureAwait(false);
    }

    public string ComputeTestCaseDiff(
        TestCaseExecutionResult testCaseExecutionResult,
        IClassExecutionSummary classExecutionSummary,
        PerformanceRunResult preloadedLastRun,
        CancellationToken cancellationToken)
    {
        var beforeIds = new[] { testCaseExecutionResult.TestInstanceContainer?.TestCaseId.DisplayName ?? string.Empty };
        var afterIds = new[] { testCaseExecutionResult.TestInstanceContainer?.TestCaseId.DisplayName ?? string.Empty };

        var beforeTestData = new TestData(
            beforeIds,
            new[] { preloadedLastRun });

        var afterTestData = new TestData(afterIds, classExecutionSummary.CompiledTestCaseResults
            .Select(x => x.PerformanceRunResult!)
            .Where(x => x.DisplayName == testCaseExecutionResult.TestInstanceContainer?.TestCaseId.DisplayName));

        var testResults = statisticalTestComputer.ComputeTest(beforeTestData, afterTestData, runSettings.SailDiffSettings);

        return testResults.Count > 0
            ? consoleWriter.WriteTestResultsToIdeConsole(testResults.Single(), new TestIds(beforeIds, afterIds), runSettings.SailDiffSettings)
            : "Current or previous runs not suitable for statistical testing";
    }
}