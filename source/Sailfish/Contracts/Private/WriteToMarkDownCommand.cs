using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using MediatR;
using Sailfish.Execution;

namespace Sailfish.Contracts.Private;

internal class WriteToMarkDownCommand : INotification
{
    public WriteToMarkDownCommand(List<IExecutionSummary> content, string outputDirectory, DateTime timeStamp, OrderedDictionary tags, OrderedDictionary args, IRunSettings settings)
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
    public OrderedDictionary Tags { get; set; }
    public OrderedDictionary Args { get; }
    public IRunSettings Settings { get; }
}