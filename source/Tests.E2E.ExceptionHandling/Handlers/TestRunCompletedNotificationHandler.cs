using MediatR;
using Sailfish.Contracts.Public.Notifications;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.E2E.ExceptionHandling.Handlers;

public class TestRunCompletedNotificationHandler : INotificationHandler<TestRunCompletedNotification>
{
    public async Task Handle(TestRunCompletedNotification notification, CancellationToken cancellationToken)
    {
        var classExecutionSummaries = notification.ClassExecutionSummaries.ToList();
        var successes = classExecutionSummaries.SelectMany(x => x.GetSuccessfulTestCases()).ToList();
        var failures = classExecutionSummaries.SelectMany(x => x.GetFailedTestCases()).ToList();

        if (failures.Any())
            foreach (var failure in failures)
            {
                var exceptions = failure.Exception;
            }

        await Task.CompletedTask;
    }
}