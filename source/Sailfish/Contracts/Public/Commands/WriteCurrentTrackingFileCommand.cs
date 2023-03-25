using System;
using System.Collections.Specialized;
using MediatR;
using Sailfish.Presentation;

namespace Sailfish.Contracts.Public.Commands;

public class WriteCurrentTrackingFileCommand : INotification
{
    public WriteCurrentTrackingFileCommand(
        ITrackingDataFormats trackingDataFormats,
        string trackingFileTrackingFileContent,
        string localOutputDirectory,
        DateTime timeStamp,
        OrderedDictionary tags,
        OrderedDictionary args)
    {
        TrackingDataFormats = trackingDataFormats;
        TrackingFileContent = trackingFileTrackingFileContent;
        LocalOutputDirectory = localOutputDirectory;
        Tags = tags;
        Args = args;
        DefaultFileName = DefaultFileSettings.DefaultTrackingFileName(timeStamp);
    }

    public ITrackingDataFormats TrackingDataFormats { get; }
    public string TrackingFileContent { get; }
    public string LocalOutputDirectory { get; }
    public OrderedDictionary Tags { get; }
    public OrderedDictionary Args { get; }
    public string DefaultFileName { get; }
}