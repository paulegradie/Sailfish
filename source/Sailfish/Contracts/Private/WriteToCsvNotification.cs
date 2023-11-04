using System;
using System.Collections.Generic;
using MediatR;
using Sailfish.Execution;

namespace Sailfish.Contracts.Private;

internal class WriteToCsvNotification : INotification
{
    public WriteToCsvNotification(List<IClassExecutionSummary> content, DateTime timeStamp)
    {
        Content = content;
        TimeStamp = timeStamp;
    }

    public List<IClassExecutionSummary> Content { get; }
    public DateTime TimeStamp { get; }
}