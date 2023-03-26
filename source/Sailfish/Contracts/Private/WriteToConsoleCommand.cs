using System.Collections.Generic;
using MediatR;
using Sailfish.Execution;
using Sailfish.Extensions.Types;


namespace Sailfish.Contracts.Private;

internal class WriteToConsoleCommand : INotification
{
    public WriteToConsoleCommand(List<IExecutionSummary> content, OrderedDictionary tags, IRunSettings settings)
    {
        Content = content;
        Tags = tags;
        Settings = settings;
    }

    public List<IExecutionSummary> Content { get; }
    public OrderedDictionary Tags { get; }
    public IRunSettings Settings { get; }
}