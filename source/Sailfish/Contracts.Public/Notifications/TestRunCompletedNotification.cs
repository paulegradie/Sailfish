using System.Collections.Generic;
using MediatR;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;

namespace Sailfish.Contracts.Public.Notifications;

public record TestRunCompletedNotification(
    IEnumerable<ClassExecutionSummaryTrackingFormat> ClassExecutionSummaries)
    : INotification;