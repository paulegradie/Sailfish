using MediatR;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;

namespace Sailfish.Contracts.Public.Notifications;

public class TestCaseCompletedNotification(ClassExecutionSummaryTrackingFormat testCaseExecutionResult) : INotification
{
    public ClassExecutionSummaryTrackingFormat TestCaseExecutionResult { get; } = testCaseExecutionResult;
}