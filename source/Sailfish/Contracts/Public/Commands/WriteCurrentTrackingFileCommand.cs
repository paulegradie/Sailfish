using System;
using Accord.Collections;
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
        OrderedDictionary<string, string> tags,
        OrderedDictionary<string, string> args)
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
    public OrderedDictionary<string, string> Tags { get; }
    public OrderedDictionary<string, string> Args { get; }
    public string DefaultFileName { get; }
}