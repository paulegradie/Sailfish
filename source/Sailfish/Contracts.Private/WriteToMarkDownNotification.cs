using System.Collections.Generic;
using MediatR;
using Sailfish.Execution;

namespace Sailfish.Contracts.Private;

internal class WriteToMarkDownNotification(List<IClassExecutionSummary> classExecutionSummaries) : INotification
{
    public List<IClassExecutionSummary> ClassExecutionSummaries { get; } = classExecutionSummaries;
}