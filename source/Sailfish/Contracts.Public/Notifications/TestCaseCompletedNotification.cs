using MediatR;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;

namespace Sailfish.Contracts.Public.Notifications;

public class TestCaseCompletedNotification : INotification
{
    public TestCaseCompletedNotification(ClassExecutionSummaryTrackingFormat testCaseExecutionResult)
    {
        TestCaseExecutionResult = testCaseExecutionResult;
    }

    public ClassExecutionSummaryTrackingFormat TestCaseExecutionResult { get; }
}