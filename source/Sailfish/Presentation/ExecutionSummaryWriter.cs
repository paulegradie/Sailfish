using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Private;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Execution;

namespace Sailfish.Presentation;

internal class ExecutionSummaryWriter : IExecutionSummaryWriter
{
    private readonly IMediator mediator;

    public ExecutionSummaryWriter(IMediator mediator)
    {
        this.mediator = mediator;
    }

    public async Task Write(
        List<IClassExecutionSummary> executionSummaries,
        DateTime timeStamp,
        string trackingDir,
        IRunSettings runSettings,
        CancellationToken cancellationToken)
    {
        await mediator.Publish(
                new WriteToConsoleNotification(
                    executionSummaries,
                    runSettings.Tags,
                    runSettings),
                cancellationToken)
            .ConfigureAwait(false);

        await mediator.Publish(
                new WriteToMarkDownNotification(
                    executionSummaries,
                    runSettings.LocalOutputDirectory ?? DefaultFileSettings.DefaultOutputDirectory,
                    timeStamp,
                    runSettings.Tags,
                    runSettings.Args,
                    runSettings),
                cancellationToken)
            .ConfigureAwait(false);

        await mediator.Publish(
                new WriteToCsvNotification(
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
            await mediator.Publish(
                    new WriteCurrentTrackingFileCommand(
                        executionSummaries.ToTrackingFormat(),
                        trackingDir,
                        timeStamp,
                        runSettings.Tags,
                        runSettings.Args),
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
}