using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Analysis;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Presentation;
using Sailfish.Presentation.Console;

namespace Sailfish.TestAdapter.Execution;

public class AdapterTestResultAnalyzer : ITestResultAnalyzer
{
    private readonly IMediator mediator;
    private readonly IConsoleWriter consoleWriter;
    private readonly ITestComputer testComputer;
    private readonly ITestResultTableContentFormatter testResultTableContentFormatter;

    public AdapterTestResultAnalyzer(
        IMediator mediator,
        IConsoleWriter consoleWriter,
        ITestComputer testComputer,
        ITestResultTableContentFormatter testResultTableContentFormatter)
    {
        this.mediator = mediator;
        this.consoleWriter = consoleWriter;
        this.testComputer = testComputer;
        this.testResultTableContentFormatter = testResultTableContentFormatter;
    }

    public Task Analyze(DateTime timeStamp, IRunSettings runSettings, string trackingDir, CancellationToken cancellationToken)
    {
        var beforeAndAfterFileLocations = mediator.Send(
            new BeforeAndAfterFileLocationCommand(
                trackingDir,
                runSettings.Tags,
                runSettings.ProvidedBeforeTrackingFiles,
                runSettings.Args),
            cancellationToken).GetAwaiter().GetResult();

        var beforeAndAfterData = mediator.Send(
                new ReadInBeforeAndAfterDataCommand(
                    beforeAndAfterFileLocations.BeforeFilePaths,
                    beforeAndAfterFileLocations.AfterFilePaths,
                    runSettings.Tags,
                    runSettings.Args),
                cancellationToken)
            .GetAwaiter().GetResult();

        if (beforeAndAfterData.BeforeData is null || beforeAndAfterData.AfterData is null)
        {
            consoleWriter.WriteString("Failed to retrieve tracking data... aborting the test operation");
            return Task.CompletedTask;
        }

        var testResults = testComputer.ComputeTest(
            beforeAndAfterData.BeforeData,
            beforeAndAfterData.AfterData,
            runSettings.Settings);

        if (!testResults.Any())
        {
            consoleWriter.WriteString("No prior test results found for the current set");
            return Task.CompletedTask;
        }

        var testIds = new TestIds(beforeAndAfterData.BeforeData.TestIds, beforeAndAfterData.AfterData.TestIds);
        var testResultFormats = testResultTableContentFormatter.CreateTableFormats(testResults, testIds, cancellationToken);

        consoleWriter.WriteStatTestResultsToConsole(testResultFormats.MarkdownFormat, testIds, runSettings.Settings);

        mediator.Publish(
                new WriteTestResultsAsMarkdownCommand(
                    testResultFormats.MarkdownFormat,
                    runSettings.LocalOutputDirectory ?? DefaultFileSettings.DefaultOutputDirectory,
                    runSettings.Settings,
                    timeStamp,
                    runSettings.Tags,
                    runSettings.Args),
                cancellationToken)
            .ConfigureAwait(false).GetAwaiter().GetResult();
        return Task.CompletedTask;
    }
}