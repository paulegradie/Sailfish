using System;
using System.Collections.Generic;
using MediatR;
using Sailfish.Statistics;

namespace Sailfish.Contracts.Private;

internal class WriteToCsvCommand : INotification
{
    public WriteToCsvCommand(List<ExecutionSummary> content, string outputDirectory, DateTime timeStamp)
    {
        Content = content;
        OutputDirectory = outputDirectory;
        TimeStamp = timeStamp;
    }

    public List<ExecutionSummary> Content { get; set; }
    public string OutputDirectory { get; set; }
    public DateTime TimeStamp { get; }
}