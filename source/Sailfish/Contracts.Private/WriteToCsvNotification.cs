using System.Collections.Generic;
using MediatR;
using Sailfish.Execution;

namespace Sailfish.Contracts.Private;

internal class WriteToCsvNotification : INotification
{
    public WriteToCsvNotification(List<IClassExecutionSummary> classExecutionSummaries)
    {
        ClassExecutionSummaries = classExecutionSummaries;
    }

    public List<IClassExecutionSummary> ClassExecutionSummaries { get; }
}