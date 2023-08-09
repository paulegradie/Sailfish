using System;
using MediatR;
using Sailfish.Extensions.Types;
using Sailfish.Presentation;

namespace Sailfish.Contracts.Public.Commands;

public class WriteCurrentScalefishResultCommand : INotification
{
    public WriteCurrentScalefishResultCommand(
        string scalefishResultMarkdown,
        string localOutputDirectory,
        DateTime timeStamp,
        OrderedDictionary tags,
        OrderedDictionary args)
    {
        ScalefishResultMarkdown = scalefishResultMarkdown;
        LocalOutputDirectory = localOutputDirectory;
        TimeStamp = timeStamp;
        Tags = tags;
        Args = args;
        DefaultFileName = DefaultFileSettings.DefaultScalefishFileName(timeStamp);
    }

    public string ScalefishResultMarkdown { get; }
    public string LocalOutputDirectory { get; }
    public DateTime TimeStamp { get; }
    public OrderedDictionary Tags { get; }
    public OrderedDictionary Args { get; }
    public string DefaultFileName { get; }
}