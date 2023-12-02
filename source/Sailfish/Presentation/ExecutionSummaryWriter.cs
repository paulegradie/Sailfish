using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Contracts.Private;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;

namespace Sailfish.Presentation;

internal interface IExecutionSummaryWriter
{
    Task Write(List<IClassExecutionSummary> executionSummaries, CancellationToken cancellationToken);
}

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
        CancellationToken cancellationToken)
    {
        await mediator.Publish(new WriteToConsoleNotification(executionSummaries), cancellationToken).ConfigureAwait(false);
        await mediator.Publish(new WriteToMarkDownNotification(executionSummaries), cancellationToken).ConfigureAwait(false);
        await mediator.Publish(new WriteToCsvNotification(executionSummaries), cancellationToken).ConfigureAwait(false);
    }
}