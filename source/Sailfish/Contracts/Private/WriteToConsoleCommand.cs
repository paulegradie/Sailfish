using System.Collections.Generic;
using MediatR;
using Sailfish.Statistics;

namespace Sailfish.Contracts.Private;

internal class WriteToConsoleCommand : INotification
{
    public WriteToConsoleCommand(List<CompiledResultContainer> content)
    {
        Content = content;
    }
    public List<CompiledResultContainer> Content { get; set; }
}