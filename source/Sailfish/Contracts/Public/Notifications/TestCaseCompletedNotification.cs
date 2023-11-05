using System;
using MediatR;
using Sailfish.Contracts.Serialization.V1;
using Sailfish.Presentation;

namespace Sailfish.Contracts.Public.Notifications;

public class TestCaseCompletedNotification : INotification
{
    public TestCaseCompletedNotification(ClassExecutionSummaryTrackingFormat testCaseExecutionResult)
    {
        TestCaseExecutionResult = testCaseExecutionResult;
    }

    public ClassExecutionSummaryTrackingFormat TestCaseExecutionResult { get; }
}