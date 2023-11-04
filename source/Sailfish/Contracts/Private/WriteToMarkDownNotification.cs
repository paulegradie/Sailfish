using System;
using System.Collections.Generic;
using MediatR;
using Sailfish.Execution;
using Sailfish.Extensions.Types;

namespace Sailfish.Contracts.Private;

internal class WriteToMarkDownNotification : INotification
{
    public WriteToMarkDownNotification(List<IClassExecutionSummary> content, DateTime timeStamp)
    {
        Content = content;
        TimeStamp = timeStamp;
    }

    public List<IClassExecutionSummary> Content { get; }
    public DateTime TimeStamp { get; }

}