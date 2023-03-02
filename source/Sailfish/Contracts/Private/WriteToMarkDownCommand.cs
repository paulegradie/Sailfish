using System;
using System.Collections.Generic;
using Accord.Collections;
using MediatR;
using Sailfish.Execution;

namespace Sailfish.Contracts.Private;

internal class WriteToMarkDownCommand : INotification
{
    public WriteToMarkDownCommand(List<IExecutionSummary> content, string outputDirectory, DateTime timeStamp, OrderedDictionary<string, string> tags, OrderedDictionary<string, string> args, IRunSettings settings)
    {
        Content = content;
        OutputDirectory = outputDirectory;
        TimeStamp = timeStamp;
        Tags = tags;
        Args = args;
        Settings = settings;
    }

    public List<IExecutionSummary> Content { get; set; }
    public string OutputDirectory { get; set; }
    public DateTime TimeStamp { get; }
    public OrderedDictionary<string, string> Tags { get; set; }
    public OrderedDictionary<string, string> Args { get; }
    public IRunSettings Settings { get; }
}