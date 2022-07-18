using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Private;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Presentation.Csv;
using Sailfish.Presentation.TTest;
using Sailfish.Statistics;
using Sailfish.Statistics.StatisticalAnalysis;

namespace Sailfish.Presentation;

internal class TestResultPresenter : ITestResultPresenter
{
    private readonly IMediator mediator;
    private readonly IPerformanceCsvTrackingWriter performanceCsvTrackingWriter;
    private readonly ITwoTailedTTestWriter twoTailedTTestWriter;

    public TestResultPresenter(
        IMediator mediator,
        IPerformanceCsvTrackingWriter performanceCsvTrackingWriter,
        ITwoTailedTTestWriter twoTailedTTestWriter)
    {
        this.mediator = mediator;
        this.performanceCsvTrackingWriter = performanceCsvTrackingWriter;
        this.twoTailedTTestWriter = twoTailedTTestWriter;
    }

    public async Task PresentResults(List<ExecutionSummary> resultContainers, DateTime timeStamp, RunSettings runSettings)
    {
        await mediator.Publish(new WriteToConsoleCommand(resultContainers, runSettings.Tags));
        await mediator.Publish(new WriteToMarkDownCommand(resultContainers, runSettings.DirectoryPath, timeStamp, runSettings.Tags, runSettings.Args));
        await mediator.Publish(new WriteToCsvCommand(resultContainers, runSettings.DirectoryPath, timeStamp, runSettings.Tags, runSettings.Args));

        var trackingDir = string.IsNullOrEmpty(runSettings.TrackingDirectoryPath) ? Path.Combine(runSettings.DirectoryPath, "tracking_output") : runSettings.TrackingDirectoryPath;
        if (!runSettings.NoTrack)
        {
            var trackingContent = await performanceCsvTrackingWriter.ConvertToCsvStringContent(resultContainers);
            await mediator.Publish(
                new WriteCurrentTrackingFileCommand(
                    trackingContent,
                    trackingDir,
                    timeStamp,
                    runSettings.Tags));
        }

        if (runSettings.Analyze)
        {
            var response = await mediator.Send(new BeforeAndAfterFileLocationCommand(trackingDir, runSettings.Tags, runSettings.BeforeTarget));
            if (string.IsNullOrEmpty(response.BeforeFilePath) || string.IsNullOrEmpty(response.AfterFilePath)) return;

            var tTestFormats = await twoTailedTTestWriter.ComputeAndConvertToStringContent(new BeforeAndAfterTrackingFiles(response.BeforeFilePath, response.AfterFilePath), runSettings.Settings);
            await mediator.Publish(new WriteTTestResultAsMarkdownCommand(tTestFormats.MarkdownTable, runSettings.DirectoryPath, runSettings.Settings, timeStamp, runSettings.Tags));
            await mediator.Publish(new WriteTTestResultAsCsvCommand(tTestFormats.CsvRows, runSettings.DirectoryPath, runSettings.Settings, timeStamp, runSettings.Tags));

            if (runSettings.Notify)
            {
                await mediator.Publish(new NotifyOnTestResultCommand(tTestFormats, runSettings.Settings, timeStamp, runSettings.Tags));
            }
        }
    }
}