using System;
using System.Collections.Generic;
using MediatR;
using Sailfish.Execution;
using Sailfish.Extensions.Types;
using Sailfish.Presentation;

namespace Sailfish.Contracts.Public.Commands;

public class WriteCurrentTrackingFileCommand : INotification
{
    public WriteCurrentTrackingFileCommand(
        IEnumerable<IClassExecutionSummary> classExecutionSummaries,
        string localOutputDirectory,
        DateTime timeStamp,
        OrderedDictionary tags,
        OrderedDictionary args)
    {
        ClassExecutionSummaries = classExecutionSummaries;
        LocalOutputDirectory = localOutputDirectory;
        Tags = tags;
        Args = args;
        DefaultFileName = DefaultFileSettings.DefaultTrackingFileName(timeStamp);
    }

    public IEnumerable<IClassExecutionSummary> ClassExecutionSummaries { get; }
    public string LocalOutputDirectory { get; }
    public OrderedDictionary Tags { get; }
    public OrderedDictionary Args { get; }
    public string DefaultFileName { get; }
}