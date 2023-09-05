using System.Collections.Generic;
using MediatR;
using Sailfish.Execution;
using Sailfish.Extensions.Types;


namespace Sailfish.Contracts.Private;

internal class WriteToConsoleCommand : INotification
{
    public WriteToConsoleCommand(List<IClassExecutionSummary> content, OrderedDictionary tags, IRunSettings settings)
    {
        Content = content;
        Tags = tags;
        Settings = settings;
    }

    public List<IClassExecutionSummary> Content { get; }
    public OrderedDictionary Tags { get; }
    public IRunSettings Settings { get; }
}