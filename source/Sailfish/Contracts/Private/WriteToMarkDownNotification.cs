using System.Collections.Generic;
using MediatR;
using Sailfish.Execution;

namespace Sailfish.Contracts.Private;

internal class WriteToMarkDownNotification : INotification
{
    public WriteToMarkDownNotification(List<IClassExecutionSummary> classExecutionSummaries)
    {
        ClassExecutionSummaries = classExecutionSummaries;
    }

    public List<IClassExecutionSummary> ClassExecutionSummaries { get; }
}