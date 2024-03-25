using System.Collections.Generic;
using MediatR;
using Sailfish.Execution;

namespace Sailfish.Contracts.Private;

internal class WriteToConsoleNotification(List<IClassExecutionSummary> content) : INotification
{
    public List<IClassExecutionSummary> Content { get; } = content;
}