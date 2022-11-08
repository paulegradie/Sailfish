using System;
using System.Collections.Generic;
using Accord.Collections;
using MediatR;
using Sailfish.Execution;

namespace Sailfish.Contracts.Private;

internal class WriteToCsvCommand : INotification
{
    public WriteToCsvCommand(
        List<ExecutionSummary> content,
        string outputDirectory,
        DateTime timeStamp,
        OrderedDictionary<string, string> tags,
        OrderedDictionary<string, string> args,
        RunSettings settings)
    {
        Content = content;
        OutputDirectory = outputDirectory;
        TimeStamp = timeStamp;
        Tags = tags;
        Args = args;
        Settings = settings;
    }

    public List<ExecutionSummary> Content { get; set; }
    public string OutputDirectory { get; set; }
    public DateTime TimeStamp { get; }
    public OrderedDictionary<string, string> Tags { get; }
    public OrderedDictionary<string, string> Args { get; }
    public RunSettings Settings { get; }
}