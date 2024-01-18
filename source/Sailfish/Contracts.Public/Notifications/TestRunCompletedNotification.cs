using MediatR;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using System.Collections.Generic;

namespace Sailfish.Contracts.Public.Notifications;

public class TestRunCompletedNotification(IEnumerable<ClassExecutionSummaryTrackingFormat> classExecutionSummaries) : INotification
{
    public IEnumerable<ClassExecutionSummaryTrackingFormat> ClassExecutionSummaries { get; } = classExecutionSummaries;
}