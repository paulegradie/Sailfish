using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Private;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Presentation.Csv;
using Sailfish.Presentation.TTest;
using Sailfish.Statistics;
using Sailfish.Statistics.StatisticalAnalysis;

namespace Sailfish.Presentation;

internal class TestResultPresenter : ITestResultPresenter
{
    private readonly IMediator mediator;
    private readonly IBeforeAndAfterStreamReader beforeAndAfterStreamReader;
    private readonly IPerformanceCsvTrackingWriter performanceCsvTrackingWriter;
    private readonly ITwoTailedTTestWriter twoTailedTTestWriter;

    public TestResultPresenter(
        IMediator mediator,
        IBeforeAndAfterStreamReader beforeAndAfterStreamReader,
        IPerformanceCsvTrackingWriter performanceCsvTrackingWriter,
        ITwoTailedTTestWriter twoTailedTTestWriter)
    {
        this.mediator = mediator;
        this.beforeAndAfterStreamReader = beforeAndAfterStreamReader;
        this.performanceCsvTrackingWriter = performanceCsvTrackingWriter;
        this.twoTailedTTestWriter = twoTailedTTestWriter;
    }

    public async Task PresentResults(
        List<CompiledResultContainer> resultContainers,
        string directoryPath,
        DateTime timeStamp,
        bool noTrack,
        bool analyze,
        TTestSettings testSettings)
    {
        await mediator.Publish(new WriteToConsoleCommand(resultContainers));
        await mediator.Publish(new WriteToMarkDownCommand(resultContainers, directoryPath, timeStamp));
        await mediator.Publish(new WriteToCsvCommand(resultContainers, directoryPath, timeStamp));

        if (!noTrack)
        {
            var trackingContent = await performanceCsvTrackingWriter.ConvertToCsvStringContent(resultContainers);
            await mediator.Publish(new WriteCurrentTrackingFileCommand(trackingContent, directoryPath, timeStamp));
        }

        if (analyze)
        {
            var response = await mediator.Send(new BeforeAndAfterFileLocationCommand(directoryPath));
            if (string.IsNullOrEmpty(response.BeforeFilePath) || string.IsNullOrEmpty(response.AfterFilePath)) return;

            var tTestContent = await twoTailedTTestWriter.ComputeAndConvertToStringContent(new BeforeAndAfterTrackingFiles(response.BeforeFilePath, response.AfterFilePath), testSettings);
            await mediator.Publish(new WriteTTestResultCommand(tTestContent, directoryPath, testSettings, timeStamp));
        }
    }
}