using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Private;
using Sailfish.Execution;

namespace Sailfish.Presentation;

internal interface IExecutionSummaryWriter
{
    Task Write(List<IClassExecutionSummary> executionSummaries, CancellationToken cancellationToken);
}

internal class ExecutionSummaryWriter : IExecutionSummaryWriter
{
    private readonly IMediator _mediator;

    public ExecutionSummaryWriter(IMediator mediator)
    {
        this._mediator = mediator;
    }

    public async Task Write(
        List<IClassExecutionSummary> executionSummaries,
        CancellationToken cancellationToken)
    {
        await _mediator.Publish(new WriteToConsoleNotification(executionSummaries), cancellationToken).ConfigureAwait(false);
        await _mediator.Publish(new WriteToMarkDownNotification(executionSummaries), cancellationToken).ConfigureAwait(false);
        await _mediator.Publish(new WriteToCsvNotification(executionSummaries), cancellationToken).ConfigureAwait(false);
    }
}