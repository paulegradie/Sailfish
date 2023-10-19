using MediatR;
using Sailfish.Contracts.Public.Commands;

namespace Tests.E2E.ExceptionHandling.Handlers;

public class WriteTrackingDataHandler : INotificationHandler<WriteCurrentTrackingFileCommand>
{
    public async Task Handle(WriteCurrentTrackingFileCommand notification, CancellationToken cancellationToken)
    {
        var classExecutionSummaries = notification.ClassExecutionSummaries.ToList();
        var successes = classExecutionSummaries.SelectMany(x => x.GetSuccessfulTestCases());
        var failures = classExecutionSummaries.SelectMany(x => x.GetFailedTestCases());
        await Task.CompletedTask;
    }
}