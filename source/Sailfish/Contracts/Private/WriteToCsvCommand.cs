using System;
using System.Collections.Generic;
using Accord.Collections;
using MediatR;
using Sailfish.Statistics;

namespace Sailfish.Contracts.Private;

internal class WriteToCsvCommand : INotification
{
    public WriteToCsvCommand(List<ExecutionSummary> content, string outputDirectory, DateTime timeStamp, OrderedDictionary<string, string> tags, OrderedDictionary<string, string> args)
    {
        Content = content;
        OutputDirectory = outputDirectory;
        TimeStamp = timeStamp;
        Tags = tags;
        Args = args;
    }

    public List<ExecutionSummary> Content { get; set; }
    public string OutputDirectory { get; set; }
    public DateTime TimeStamp { get; }
    public OrderedDictionary<string, string> Tags { get; }
    public OrderedDictionary<string, string> Args { get; }
}