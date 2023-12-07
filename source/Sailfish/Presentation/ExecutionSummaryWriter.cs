using MediatR;
using Sailfish.Contracts.Private;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Presentation;

internal interface IExecutionSummaryWriter
{
    Task Write(List<IClassExecutionSummary> executionSummaries, CancellationToken cancellationToken);
}

internal class ExecutionSummaryWriter(IMediator mediator, IRunSettings runSettings) : IExecutionSummaryWriter
{
    private readonly IMediator mediator = mediator;

    public async Task Write(
        List<IClassExecutionSummary> executionSummaries,
        CancellationToken cancellationToken)
    {
        await mediator.Publish(new WriteToConsoleNotification(executionSummaries), cancellationToken).ConfigureAwait(false);
        await mediator.Publish(new WriteToMarkDownNotification(executionSummaries), cancellationToken).ConfigureAwait(false);
        await mediator.Publish(new WriteToCsvNotification(executionSummaries), cancellationToken).ConfigureAwait(false);
    }
}