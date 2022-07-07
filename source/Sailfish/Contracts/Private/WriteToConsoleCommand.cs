using System.Collections.Generic;
using Accord.Collections;
using MediatR;
using Sailfish.Statistics;

namespace Sailfish.Contracts.Private;

internal class WriteToConsoleCommand : INotification
{
    public WriteToConsoleCommand(List<ExecutionSummary> content, OrderedDictionary<string, string> tags)
    {
        Content = content;
        Tags = tags;
    }

    public List<ExecutionSummary> Content { get; set; }
    public OrderedDictionary<string, string> Tags { get; }
}