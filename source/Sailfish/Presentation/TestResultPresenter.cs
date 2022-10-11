using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Private;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Presentation.Csv;
using Sailfish.Presentation.TTest;
using Sailfish.Statistics;
using Serilog;

namespace Sailfish.Presentation;

internal class TestResultPresenter : ITestResultPresenter
{
    private readonly ILogger logger;
    private readonly IMediator mediator;
    private readonly IPerformanceCsvTrackingWriter performanceCsvTrackingWriter;
    private readonly ITwoTailedTTestWriter twoTailedTTestWriter;

    public TestResultPresenter(
        ILogger logger,
        IMediator mediator,
        IPerformanceCsvTrackingWriter performanceCsvTrackingWriter,
        ITwoTailedTTestWriter twoTailedTTestWriter)
    {
        this.logger = logger;
        this.mediator = mediator;
        this.performanceCsvTrackingWriter = performanceCsvTrackingWriter;
        this.twoTailedTTestWriter = twoTailedTTestWriter;
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
                    runSettings.Tags),
                cancellationToken)
            .ConfigureAwait(false);

        await mediator.Publish(
                new WriteToMarkDownCommand(
                    resultContainers,
                    runSettings.DirectoryPath,
                    timeStamp,
                    runSettings.Tags,
                    runSettings.Args),
                cancellationToken)
            .ConfigureAwait(false);

        await mediator.Publish(
                new WriteToCsvCommand(
                    resultContainers,
                    runSettings.DirectoryPath,
                    timeStamp,
                    runSettings.Tags,
                    runSettings.Args),
                cancellationToken)
            .ConfigureAwait(false);

        var trackingDir = string.IsNullOrEmpty(runSettings.TrackingDirectoryPath)
            ? Path.Combine(runSettings.DirectoryPath, "tracking_output")
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
            if (!beforeAndAfterFileLocations.BeforeFilePath.Any() || !beforeAndAfterFileLocations.AfterFilePath.Any())
            {
                logger.Fatal("Failed to identify before and after file locations when analyzing tracking data");
                return;
            }

            var beforeAndAfterData = await mediator.Send(
                    new ReadInBeforeAndAfterDataCommand(
                        beforeAndAfterFileLocations.BeforeFilePath,
                        beforeAndAfterFileLocations.AfterFilePath,
                        runSettings.BeforeTarget,
                        runSettings.Tags,
                        runSettings.Args),
                    cancellationToken)
                .ConfigureAwait(false);

            if (beforeAndAfterData.BeforeData is null || beforeAndAfterData.AfterData is null)
            {
                logger.Fatal("Failed to retrieve test result data");
                return;
            }

            var tTestResults = await twoTailedTTestWriter.ComputeAndConvertToStringContent(
                    new TestData(beforeAndAfterFileLocations.BeforeFilePath, beforeAndAfterData.BeforeData.Data),
                    new TestData(beforeAndAfterFileLocations.AfterFilePath, beforeAndAfterData.AfterData.Data),
                    runSettings.Settings,
                    cancellationToken)
                .ConfigureAwait(false);

            await mediator.Publish(
                    new WriteTTestResultAsMarkdownCommand(
                        tTestResults.MarkdownTable,
                        runSettings.DirectoryPath,
                        runSettings.Settings,
                        timeStamp,
                        runSettings.Tags,
                        runSettings.Args),
                    cancellationToken)
                .ConfigureAwait(false);

            await mediator.Publish(
                    new WriteTTestResultAsCsvCommand(
                        tTestResults.CsvRows,
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
                            tTestResults,
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