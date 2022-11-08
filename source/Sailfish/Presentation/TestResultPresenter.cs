using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Analysis;
using Sailfish.Contracts.Private;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Execution;
using Sailfish.Presentation.Csv;
using Serilog;

namespace Sailfish.Presentation;

internal class TestResultPresenter : ITestResultPresenter
{
    private readonly ILogger logger;
    private readonly IMediator mediator;
    private readonly ITestComputer testComputer;
    private readonly IPerformanceCsvTrackingWriter performanceCsvTrackingWriter;
    private readonly ITestResultTableContentFormatter testResultTableContentFormatter;

    private const string DefaultTrackingDirectory = "tracking_output";

    public TestResultPresenter(
        ILogger logger,
        IMediator mediator,
        ITestComputer testComputer,
        IPerformanceCsvTrackingWriter performanceCsvTrackingWriter,
        ITestResultTableContentFormatter testResultTableContentFormatter)
    {
        this.logger = logger;
        this.mediator = mediator;
        this.testComputer = testComputer;
        this.performanceCsvTrackingWriter = performanceCsvTrackingWriter;
        this.testResultTableContentFormatter = testResultTableContentFormatter;
    }

    public async Task PresentResults(
        List<ExecutionSummary> resultContainers,
        DateTime timeStamp,
        RunSettings runSettings,
        CancellationToken cancellationToken)
    {
        await mediator.Publish(
                new WriteToConsoleCommand(
                    resultContainers,
                    runSettings.Tags,
                    runSettings),
                cancellationToken)
            .ConfigureAwait(false);

        await mediator.Publish(
                new WriteToMarkDownCommand(
                    resultContainers,
                    runSettings.DirectoryPath,
                    timeStamp,
                    runSettings.Tags,
                    runSettings.Args,
                    runSettings),
                cancellationToken)
            .ConfigureAwait(false);

        await mediator.Publish(
                new WriteToCsvCommand(
                    resultContainers,
                    runSettings.DirectoryPath,
                    timeStamp,
                    runSettings.Tags,
                    runSettings.Args,
                    runSettings),
                cancellationToken)
            .ConfigureAwait(false);

        var trackingDir = string.IsNullOrEmpty(runSettings.TrackingDirectoryPath)
            ? Path.Combine(runSettings.DirectoryPath, DefaultTrackingDirectory)
            : runSettings.TrackingDirectoryPath;

        if (!runSettings.NoTrack)
        {
            var trackingContent = await performanceCsvTrackingWriter.ConvertToCsvStringContent(resultContainers);
            await mediator.Publish(
                    new WriteCurrentTrackingFileCommand(
                        trackingContent,
                        trackingDir,
                        timeStamp,
                        runSettings.Tags,
                        runSettings.Args),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        if (runSettings.Analyze)
        {
            var beforeAndAfterFileLocations = await mediator.Send(
                    new BeforeAndAfterFileLocationCommand(
                        trackingDir,
                        runSettings.Tags,
                        runSettings.BeforeTarget,
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

                message.Append($"If file locations are not provided, data must be provided via the {nameof(ReadInBeforeAndAfterDataCommand)} handler.");
                var msg = message.ToString();
                logger.Warning("{Message}", msg);
            }

            var beforeAndAfterData = await mediator.Send(
                    new ReadInBeforeAndAfterDataCommand(
                        beforeAndAfterFileLocations.BeforeFilePaths,
                        beforeAndAfterFileLocations.AfterFilePaths,
                        runSettings.BeforeTarget,
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
                runSettings.Settings);

            if (!testResults.Any())
            {
                logger.Information("No prior test results found for the current set");
                return;
            }

            var testIds = new TestIds(beforeAndAfterData.BeforeData.TestIds, beforeAndAfterData.AfterData.TestIds);
            var testResultFormats = testResultTableContentFormatter.CreateTableFormats(testResults, testIds, cancellationToken);
            TestResultConsoleWriter.WriteToConsole(testResultFormats.MarkdownFormat, testIds, runSettings.Settings);

            await mediator.Publish(
                    new WriteTestResultsAsMarkdownCommand(
                        testResultFormats.MarkdownFormat,
                        runSettings.DirectoryPath,
                        runSettings.Settings,
                        timeStamp,
                        runSettings.Tags,
                        runSettings.Args),
                    cancellationToken)
                .ConfigureAwait(false);

            await mediator.Publish(
                    new WriteTestResultsAsCsvCommand(
                        testResultFormats.CsvFormat,
                        runSettings.DirectoryPath,
                        runSettings.Settings,
                        timeStamp,
                        runSettings.Tags,
                        runSettings.Args),
                    cancellationToken)
                .ConfigureAwait(false);

            if (runSettings.Notify)
            {
                await mediator.Publish(
                        new NotifyOnTestResultCommand(
                            testResultFormats,
                            runSettings.Settings,
                            timeStamp,
                            runSettings.Tags,
                            runSettings.Args),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}