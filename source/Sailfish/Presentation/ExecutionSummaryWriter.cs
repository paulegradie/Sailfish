using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Private;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Execution;
using Sailfish.Presentation.CsvAndJson;

namespace Sailfish.Presentation;

internal class ExecutionSummaryWriter : IExecutionSummaryWriter
{
    private readonly IMediator mediator;
    private readonly IPerformanceResultPresenter performanceResultPresenter;

    public ExecutionSummaryWriter(
        IMediator mediator,
        IPerformanceResultPresenter performanceResultPresenter)
    {
        this.mediator = mediator;
        this.performanceResultPresenter = performanceResultPresenter;
    }

    public async Task Write(
        List<IExecutionSummary> executionSummaries,
        DateTime timeStamp,
        string trackingDir,
        IRunSettings runSettings,
        CancellationToken cancellationToken)
    {
        await mediator.Publish(
                new WriteToConsoleCommand(
                    executionSummaries,
                    runSettings.Tags,
                    runSettings),
                cancellationToken)
            .ConfigureAwait(false);

        await mediator.Publish(
                new WriteToMarkDownCommand(
                    executionSummaries,
                    runSettings.LocalOutputDirectory ?? DefaultFileSettings.DefaultOutputDirectory,
                    timeStamp,
                    runSettings.Tags,
                    runSettings.Args,
                    runSettings),
                cancellationToken)
            .ConfigureAwait(false);

        await mediator.Publish(
                new WriteToCsvCommand(
                    executionSummaries,
                    runSettings.LocalOutputDirectory ?? DefaultFileSettings.DefaultOutputDirectory,
                    timeStamp,
                    runSettings.Tags,
                    runSettings.Args,
                    runSettings),
                cancellationToken)
            .ConfigureAwait(false);

        if (runSettings.CreateTrackingFiles)
        {
            var trackingDataAsCsv = await performanceResultPresenter.ConvertToCsvStringContent(executionSummaries, cancellationToken);
            var trackingDataAsJson = await performanceResultPresenter.ConvertToJson(executionSummaries, cancellationToken);
            var trackingDataFormats = new TrackingDataFormats(trackingDataAsJson, trackingDataAsCsv, executionSummaries);

            await mediator.Publish(
                    new WriteCurrentTrackingFileCommand(
                        trackingDataFormats,
                        trackingDataAsCsv,
                        trackingDir,
                        timeStamp,
                        runSettings.Tags,
                        runSettings.Args),
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
}