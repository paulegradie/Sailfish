using System;
using System.Collections.Generic;
using MediatR;
using Sailfish.Contracts.Serialization.V1;
using Sailfish.Extensions.Types;
using Sailfish.Presentation;

namespace Sailfish.Contracts.Public.Commands;

public class WriteCurrentTrackingFileCommand : INotification
{
    public WriteCurrentTrackingFileCommand(
        IEnumerable<ClassExecutionSummaryTrackingFormat> classExecutionSummaries,
        DateTime timeStamp,
        OrderedDictionary tags,
        OrderedDictionary args)
    {
        ClassExecutionSummaries = classExecutionSummaries;
        Tags = tags;
        Args = args;
        DefaultFileName = DefaultFileSettings.DefaultTrackingFileName(timeStamp);
    }

    public IEnumerable<ClassExecutionSummaryTrackingFormat> ClassExecutionSummaries { get; }
    public OrderedDictionary Tags { get; }
    public OrderedDictionary Args { get; }
    public string DefaultFileName { get; }
}