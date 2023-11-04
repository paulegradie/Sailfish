using System;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Contracts.Private;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Execution;

namespace Sailfish.Presentation;

internal class ExecutionSummaryWriter : IExecutionSummaryWriter
{
    private readonly IMediator mediator;
    private readonly IRunSettings runSettings;

    public ExecutionSummaryWriter(IMediator mediator, IRunSettings runSettings)
    {
        this.mediator = mediator;
        this.runSettings = runSettings;
    }

    public async Task Write(
        List<IClassExecutionSummary> executionSummaries,
        DateTime timeStamp,
        CancellationToken cancellationToken)
    {
        await mediator.Publish(new WriteToConsoleNotification(executionSummaries), cancellationToken).ConfigureAwait(false);
        await mediator.Publish(new WriteToMarkDownNotification(executionSummaries, timeStamp), cancellationToken).ConfigureAwait(false);
        await mediator.Publish(new WriteToCsvNotification(executionSummaries, timeStamp), cancellationToken).ConfigureAwait(false);

        if (runSettings.CreateTrackingFiles)
        {
            await mediator.Publish(new WriteCurrentTrackingFileCommand(executionSummaries.ToTrackingFormat(), timeStamp), cancellationToken).ConfigureAwait(false);
        }
    }
}