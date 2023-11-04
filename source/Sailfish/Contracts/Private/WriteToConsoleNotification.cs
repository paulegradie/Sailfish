using System.Collections.Generic;
using MediatR;
using Sailfish.Execution;


namespace Sailfish.Contracts.Private;

internal class WriteToConsoleNotification : INotification
{
    public WriteToConsoleNotification(List<IClassExecutionSummary> content)
    {
        Content = content;
    }

    public List<IClassExecutionSummary> Content { get; }
}