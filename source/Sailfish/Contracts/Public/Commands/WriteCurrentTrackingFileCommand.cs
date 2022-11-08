using System;
using Accord.Collections;
using MediatR;
using Sailfish.Presentation;

namespace Sailfish.Contracts.Public.Commands;

public class WriteCurrentTrackingFileCommand : INotification
{
    public WriteCurrentTrackingFileCommand(string trackingFileTrackingFileContent, string defaultOutputDirectory, DateTime timeStamp, OrderedDictionary<string, string> tags, OrderedDictionary<string, string> args)
    {
        TrackingFileContent = trackingFileTrackingFileContent;
        DefaultOutputDirectory = defaultOutputDirectory;
        Tags = tags;
        Args = args;
        DefaultFileName = DefaultFileSettings.DefaultTrackingFileName(timeStamp);
    }

    public string TrackingFileContent { get; set; }
    public string DefaultOutputDirectory { get; set; }
    public OrderedDictionary<string, string> Tags { get; }
    public OrderedDictionary<string, string> Args { get; }
    public string DefaultFileName { get; }
}