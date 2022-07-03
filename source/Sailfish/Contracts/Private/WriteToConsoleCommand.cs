using System.Collections.Generic;
using MediatR;
using Sailfish.Statistics;

namespace Sailfish.Contracts.Private;

internal class WriteToConsoleCommand : INotification
{
    public WriteToConsoleCommand(List<ExecutionSummary> content)
    {
        Content = content;
    }

    public List<ExecutionSummary> Content { get; set; }
}