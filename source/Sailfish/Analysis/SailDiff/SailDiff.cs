using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Contracts.Public.Requests;
using Sailfish.Presentation;
using Sailfish.Presentation.Console;
using Serilog;

namespace Sailfish.Analysis.SailDiff;

public interface ISailDiff : IAnalyzeFromFile
{
}

public class SailDiff : ISailDiff
{
    private readonly IMediator mediator;
    private readonly IRunSettings runSettings;
    private readonly ILogger logger;
    private readonly ITestComputer testComputer;
    private readonly ITestResultTableContentFormatter testResultTableContentFormatter;
    private readonly IConsoleWriter consoleWriter;

    public SailDiff(
        IMediator mediator,
        IRunSettings runSettings,
        ILogger logger,
        ITestComputer testComputer,
        ITestResultTableContentFormatter testResultTableContentFormatter,
        IConsoleWriter consoleWriter)
    {
        this.mediator = mediator;
        this.runSettings = runSettings;
        this.logger = logger;
        this.testComputer = testComputer;
        this.testResultTableContentFormatter = testResultTableContentFormatter;
        this.consoleWriter = consoleWriter;
    }

    public async Task Analyze(DateTime timeStamp, CancellationToken cancellationToken
    )
    {
        if (!runSettings.RunSailDiff) return;
        var beforeAndAfterFileLocations = await mediator.Send(
                new BeforeAndAfterFileLocationRequest(
                    runSettings.Tags,
                    runSettings.ProvidedBeforeTrackingFiles,
                    runSettings.Args),
                cancellationToken)
            .ConfigureAwait(false);

        if (!beforeAndAfterFileLocations.AfterFilePaths.Any() || !beforeAndAfterFileLocations.BeforeFilePaths.Any())
        {
            var message = new StringBuilder();
            if (!beforeAndAfterFileLocations.BeforeFilePaths.Any())
            {
                message.Append("No 'Before' file locations discovered. ");
            }

            if (!beforeAndAfterFileLocations.AfterFilePaths.Any())
            {
                message.Append("No 'After' file locations discovered. ");
            }

            message.Append(
                $"If file locations are not provided, data must be provided via the {nameof(ReadInBeforeAndAfterDataRequest)} handler.");
            var msg = message.ToString();
            logger.Warning("{Message}", msg);
        }

        var beforeAndAfterData = await mediator.Send(
                new ReadInBeforeAndAfterDataRequest(
                    beforeAndAfterFileLocations.BeforeFilePaths,
                    beforeAndAfterFileLocations.AfterFilePaths,
                    runSettings.Tags,
                    runSettings.Args),
                cancellationToken)
            .ConfigureAwait(false);

        if (beforeAndAfterData.BeforeData is null || beforeAndAfterData.AfterData is null)
        {
            logger.Warning("Failed to retrieve tracking data... aborting the test operation");
            return;
        }

        var testResults = testComputer.ComputeTest(
            beforeAndAfterData.BeforeData,
            beforeAndAfterData.AfterData,
            runSettings.SailDiffSettings);

        if (!testResults.Any())
        {
            logger.Information("No prior test results found for the current set");
            return;
        }

        var testIds = new TestIds(beforeAndAfterData.BeforeData.TestIds, beforeAndAfterData.AfterData.TestIds);
        var testResultFormats = testResultTableContentFormatter.CreateTableFormats(testResults, testIds, cancellationToken);

        consoleWriter.WriteStatTestResultsToConsole(testResultFormats.MarkdownFormat, testIds, runSettings.SailDiffSettings);

        await mediator.Publish(
                new WriteTestResultsAsMarkdownNotification(
                    testResultFormats.MarkdownFormat,
                    runSettings.LocalOutputDirectory ?? DefaultFileSettings.DefaultOutputDirectory,
                    runSettings.SailDiffSettings,
                    timeStamp,
                    runSettings.Tags,
                    runSettings.Args),
                cancellationToken)
            .ConfigureAwait(false);

        await mediator.Publish(
                new WriteTestResultsAsCsvNotification(
                    testResultFormats.CsvFormat,
                    runSettings.LocalOutputDirectory ?? DefaultFileSettings.DefaultOutputDirectory,
                    runSettings.SailDiffSettings,
                    timeStamp,
                    runSettings.Tags,
                    runSettings.Args),
                cancellationToken)
            .ConfigureAwait(false);

        if (runSettings.Notify)
        {
            await mediator.Publish(
                    new NotifyOnTestResultNotification(
                        testResultFormats,
                        runSettings.SailDiffSettings,
                        timeStamp,
                        runSettings.Tags,
                        runSettings.Args),
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
}