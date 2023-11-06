using MediatR;
using Sailfish.Contracts.Serialization.V1;

namespace Sailfish.Contracts.Public.Notifications;

public class TestCaseCompletedNotification : INotification
{
    public TestCaseCompletedNotification(ClassExecutionSummaryTrackingFormat testCaseExecutionResult)
    {
        TestCaseExecutionResult = testCaseExecutionResult;
    }

    public ClassExecutionSummaryTrackingFormat TestCaseExecutionResult { get; }
}