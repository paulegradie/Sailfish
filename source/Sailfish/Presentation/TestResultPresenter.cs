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

internal class TestResultPresenter : ITestResultPresenter
{
    private readonly IMediator mediator;
    private readonly IPerformanceResultPresenter performanceResultPresenter;

    public TestResultPresenter(
        IMediator mediator,
        IPerformanceResultPresenter performanceResultPresenter)
    {
        this.mediator = mediator;
        this.performanceResultPresenter = performanceResultPresenter;
    }

    public async Task PresentResults(
        List<IExecutionSummary> resultContainers,
        DateTime timeStamp,
        string trackingDir,
        IRunSettings runSettings,
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
                    runSettings.LocalOutputDirectory ?? DefaultFileSettings.DefaultOutputDirectory,
                    timeStamp,
                    runSettings.Tags,
                    runSettings.Args,
                    runSettings),
                cancellationToken)
            .ConfigureAwait(false);

        await mediator.Publish(
                new WriteToCsvCommand(
                    resultContainers,
                    runSettings.LocalOutputDirectory ?? DefaultFileSettings.DefaultOutputDirectory,
                    timeStamp,
                    runSettings.Tags,
                    runSettings.Args,
                    runSettings),
                cancellationToken)
            .ConfigureAwait(false);

        if (runSettings.CreateTrackingFiles)
        {
            var trackingDataAsCsv = await performanceResultPresenter.ConvertToCsvStringContent(resultContainers, cancellationToken);
            var trackingDataAsJson = await performanceResultPresenter.ConvertToJson(resultContainers, cancellationToken);
            var trackingDataFormats = new TrackingDataFormats(trackingDataAsJson, trackingDataAsCsv, resultContainers);

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