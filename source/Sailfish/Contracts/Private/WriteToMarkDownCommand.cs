using System;
using System.Collections.Generic;
using MediatR;
using Sailfish.Statistics;

namespace Sailfish.Contracts.Private;

internal class WriteToMarkDownCommand : INotification
{
    public WriteToMarkDownCommand(List<ExecutionSummary> content, string outputDirectory, DateTime timeStamp, Dictionary<string, string> tags)
    {
        Content = content;
        OutputDirectory = outputDirectory;
        TimeStamp = timeStamp;
        Tags = tags;
    }

    public List<ExecutionSummary> Content { get; set; }
    public string OutputDirectory { get; set; }
    public DateTime TimeStamp { get; }
    public Dictionary<string, string> Tags { get; set; }
}