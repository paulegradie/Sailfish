using System;
using System.Collections.Generic;
using MediatR;
using Sailfish.Execution;
using Sailfish.Extensions.Types;

namespace Sailfish.Contracts.Private;

internal class WriteToMarkDownNotification : INotification
{
    public WriteToMarkDownNotification(
        List<IClassExecutionSummary> content,
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

    public List<IClassExecutionSummary> Content { get; }
    public string OutputDirectory { get; }
    public DateTime TimeStamp { get; }
    public OrderedDictionary Tags { get; }
    public OrderedDictionary Args { get; }
    public IRunSettings Settings { get; }
}