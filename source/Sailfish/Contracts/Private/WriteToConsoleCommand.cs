using System.Collections.Generic;
using Accord.Collections;
using MediatR;
using Sailfish.Execution;

namespace Sailfish.Contracts.Private;

internal class WriteToConsoleCommand : INotification
{
    public WriteToConsoleCommand(List<IExecutionSummary> content, OrderedDictionary<string, string> tags, RunSettings settings)
    {
        Content = content;
        Tags = tags;
        Settings = settings;
    }

    public List<IExecutionSummary> Content { get; set; }
    public OrderedDictionary<string, string> Tags { get; }
    public RunSettings Settings { get; }
}