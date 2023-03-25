using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using MediatR;
using Sailfish.Execution;

namespace Sailfish.Contracts.Private;

internal class WriteToCsvCommand : INotification
{
    public WriteToCsvCommand(
        List<IExecutionSummary> content,
        string outputDirectory,
        DateTime timeStamp,
        OrderedDictionary tags,
        OrderedDictionary args,
        IRunSettings settings)
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
    public OrderedDictionary Tags { get; }
    public OrderedDictionary Args { get; }
    public IRunSettings Settings { get; }
}