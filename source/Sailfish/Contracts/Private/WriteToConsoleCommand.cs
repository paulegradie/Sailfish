using System.Collections.Generic;
using MediatR;
using Sailfish.Execution;
using System.Collections.Specialized;

namespace Sailfish.Contracts.Private;

internal class WriteToConsoleCommand : INotification
{
    public WriteToConsoleCommand(List<IExecutionSummary> content, OrderedDictionary tags, IRunSettings settings)
    {
        Content = content;
        Tags = tags;
        Settings = settings;
    }

    public List<IExecutionSummary> Content { get; set; }
    public OrderedDictionary Tags { get; }
    public IRunSettings Settings { get; }
}