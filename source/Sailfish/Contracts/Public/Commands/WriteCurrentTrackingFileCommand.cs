using System;
using System.Collections.Generic;
using MediatR;
using Sailfish.Presentation;

namespace Sailfish.Contracts.Public.Commands;

public class WriteCurrentTrackingFileCommand : INotification
{
    public WriteCurrentTrackingFileCommand(string trackingFileContent, string defaultOutputDirectory, DateTime timeStamp, Dictionary<string, string> tags)
    {
        Content = trackingFileContent;
        DefaultOutputDirectory = defaultOutputDirectory;
        Tags = tags;
        DefaultFileName = DefaultFileSettings.DefaultTrackingFileName(timeStamp);
    }

    public string Content { get; set; }
    public string DefaultOutputDirectory { get; set; }
    public Dictionary<string, string> Tags { get; }
    public string DefaultFileName { get; }
}