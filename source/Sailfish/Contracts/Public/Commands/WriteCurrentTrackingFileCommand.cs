using System;
using MediatR;
using Sailfish.Presentation;

namespace Sailfish.Contracts.Public.Commands;

public class WriteCurrentTrackingFileCommand : INotification
{
    public WriteCurrentTrackingFileCommand(string trackingFileContent, string defaultOutputDirectory, DateTime timeStamp)
    {
        Content = trackingFileContent;
        DefaultOutputDirectory = defaultOutputDirectory;
        DefaultFileName = DefaultFileSettings.DefaultTrackingFileName(timeStamp);
    }

    public string Content { get; set; }
    public string DefaultOutputDirectory { get; set; }
    public string DefaultFileName { get; }
}