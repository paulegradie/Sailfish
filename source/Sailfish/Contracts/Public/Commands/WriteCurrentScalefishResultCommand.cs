using System;
using MediatR;
using Sailfish.Extensions.Types;
using Sailfish.Presentation;

namespace Sailfish.Contracts.Public.Commands;

public class WriteCurrentScalefishResultCommand : INotification
{
    public WriteCurrentScalefishResultCommand(string scalefishResultMarkdown, DateTime timeStamp)
    {
        ScalefishResultMarkdown = scalefishResultMarkdown;
        TimeStamp = timeStamp;
        DefaultFileName = DefaultFileSettings.DefaultScalefishFileName(timeStamp);
    }

    public string ScalefishResultMarkdown { get; }
    public DateTime TimeStamp { get; }
    public string DefaultFileName { get; }
}