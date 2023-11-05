using System.Collections.Generic;
using MediatR;
using Sailfish.Contracts.Serialization.V1;

namespace Sailfish.Contracts.Public.Notifications;

public class TestRunCompletedNotification : INotification
{
    public TestRunCompletedNotification(IEnumerable<ClassExecutionSummaryTrackingFormat> classExecutionSummaries)
    {
        ClassExecutionSummaries = classExecutionSummaries;
    }

    public IEnumerable<ClassExecutionSummaryTrackingFormat> ClassExecutionSummaries { get; }
}