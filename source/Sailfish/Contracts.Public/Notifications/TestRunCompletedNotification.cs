using System.Collections.Generic;
using MediatR;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;

namespace Sailfish.Contracts.Public.Notifications;

public record TestRunCompletedNotification : INotification
{
    public TestRunCompletedNotification(IEnumerable<ClassExecutionSummaryTrackingFormat> ClassExecutionSummaries)
    {
        this.ClassExecutionSummaries = ClassExecutionSummaries;
    }

    public IEnumerable<ClassExecutionSummaryTrackingFormat> ClassExecutionSummaries { get; init; }

    public void Deconstruct(out IEnumerable<ClassExecutionSummaryTrackingFormat> ClassExecutionSummaries)
    {
        ClassExecutionSummaries = this.ClassExecutionSummaries;
    }
}