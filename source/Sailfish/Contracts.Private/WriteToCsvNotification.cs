using System.Collections.Generic;
using MediatR;
using Sailfish.Execution;

namespace Sailfish.Contracts.Private;

internal class WriteToCsvNotification(List<IClassExecutionSummary> classExecutionSummaries) : INotification
{
    public List<IClassExecutionSummary> ClassExecutionSummaries { get; } = classExecutionSummaries;
}