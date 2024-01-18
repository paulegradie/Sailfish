using MediatR;
using Sailfish.Execution;
using System.Collections.Generic;

namespace Sailfish.Contracts.Private;

internal class WriteToConsoleNotification(List<IClassExecutionSummary> content) : INotification
{
    public List<IClassExecutionSummary> Content { get; } = content;
}