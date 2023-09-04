using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Execution;
using Sailfish.Presentation;

namespace Sailfish.TestAdapter.Execution;

internal class AdapterSailDiff : IAdapterSailDiff
{
    private readonly IMediator mediator;
    private readonly IAdapterConsoleWriter consoleWriter;
    private readonly ITestComputer testComputer;
    private readonly ITestResultTableContentFormatter testResultTableContentFormatter;

    public AdapterSailDiff(
        IMediator mediator,
        IAdapterConsoleWriter consoleWriter,
        ITestComputer testComputer,
        ITestResultTableContentFormatter testResultTableContentFormatter)
    {
        this.mediator = mediator;
        this.consoleWriter = consoleWriter;
        this.testComputer = testComputer;
        this.testResultTableContentFormatter = testResultTableContentFormatter;
    }

    public async Task Analyze(DateTime timeStamp, IRunSettings runSettings, string trackingDir, CancellationToken cancellationToken)
    {
        if (!runSettings.RunSailDiff) return;

        var beforeAndAfterFileLocations = await mediator.Send(
            new BeforeAndAfterFileLocationCommand(
                trackingDir,
                runSettings.Tags,
                runSettings.ProvidedBeforeTrackingFiles,
                runSettings.Args),
            cancellationToken).ConfigureAwait(false);

        var beforeAndAfterData = await mediator.Send(
            new ReadInBeforeAndAfterDataCommand(
                beforeAndAfterFileLocations.BeforeFilePaths,
                beforeAndAfterFileLocations.AfterFilePaths,
                runSettings.Tags,
                runSettings.Args),
            cancellationToken).ConfigureAwait(false);

        if (beforeAndAfterData.BeforeData is null || beforeAndAfterData.AfterData is null)
        {
            consoleWriter.WriteString("Failed to retrieve tracking data... aborting the test operation");
            return;
        }

        var testResults = testComputer.ComputeTest(
            beforeAndAfterData.BeforeData,
            beforeAndAfterData.AfterData,
            runSettings.Settings);

        if (!testResults.Any())
        {
            consoleWriter.WriteString("No prior test results found for the current set");
            return;
        }

        var testIds = new TestIds(beforeAndAfterData.BeforeData.TestIds, beforeAndAfterData.AfterData.TestIds);
        var testResultFormats = testResultTableContentFormatter.CreateTableFormats(testResults, testIds, cancellationToken);

        consoleWriter.WriteStatTestResultsToConsole(testResultFormats.MarkdownFormat, testIds, runSettings.Settings);

        await mediator.Publish(
                new WriteTestResultsAsMarkdownCommand(
                    testResultFormats.MarkdownFormat,
                    runSettings.LocalOutputDirectory ?? DefaultFileSettings.DefaultOutputDirectory,
                    runSettings.Settings,
                    timeStamp,
                    runSettings.Tags,
                    runSettings.Args),
                cancellationToken)
            .ConfigureAwait(false);

        await mediator.Publish(
            new WriteTestResultsAsCsvCommand(testResultFormats.CsvFormat,
                runSettings.LocalOutputDirectory ?? DefaultFileSettings.DefaultOutputDirectory,
                runSettings.Settings,
                timeStamp,
                runSettings.Tags,
                runSettings.Args
            ), cancellationToken);
    }

    public string ComputeTestCaseDiff(
        TestExecutionResult testExecutionResult,
        IExecutionSummary executionSummary,
        SailDiffSettings sailDiffSettings,
        PerformanceRunResult preloadedLastRun,
        CancellationToken cancellationToken)
    {
        var beforeIds = new[] { testExecutionResult.TestInstanceContainer?.TestCaseId.DisplayName ?? string.Empty };
        var afterIds = new[] { testExecutionResult.TestInstanceContainer?.TestCaseId.DisplayName ?? string.Empty };

        var beforeTestData = new TestData(
            beforeIds,
            new[] { preloadedLastRun });

        var afterTestData = new TestData(afterIds, executionSummary.CompiledTestCaseResults
            .Select(x => x.PerformanceRunResult!)
            .Where(x => x.DisplayName == testExecutionResult.TestInstanceContainer?.TestCaseId.DisplayName));

        var testResults = testComputer.ComputeTest(beforeTestData, afterTestData, sailDiffSettings);

        return testResults.Count > 0
            ? consoleWriter.WriteTestResultsToIdeConsole(testResults.Single(), new TestIds(beforeIds, afterIds), sailDiffSettings)
            : "No prior runs found for statistical testing";
    }
}