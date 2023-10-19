using MediatR;
using Sailfish.Contracts.Public.Commands;

namespace Tests.E2E.ExceptionHandling.Handlers;

public class WriteTrackingDataHandler : INotificationHandler<WriteCurrentTrackingFileCommand>
{
    public async Task Handle(WriteCurrentTrackingFileCommand notification, CancellationToken cancellationToken)
    {
        var classExecutionSummaries = notification.ClassExecutionSummaries.ToList();
        var successes = classExecutionSummaries.SelectMany(x => x.GetSuccessfulTestCases()).ToList();
        var failures = classExecutionSummaries.SelectMany(x => x.GetFailedTestCases()).ToList();

        if (failures.Any())
        {
            foreach (var failure in failures)
            {
                var exceptions = failure.Exception;
            }
        }

        await Task.CompletedTask;
    }
}