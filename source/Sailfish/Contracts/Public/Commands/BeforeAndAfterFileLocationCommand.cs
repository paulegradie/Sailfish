using System.Collections.Generic;
using Accord.Collections;
using MediatR;

namespace Sailfish.Contracts.Public.Commands;

public class BeforeAndAfterFileLocationCommand : IRequest<BeforeAndAfterFileLocationResponse>
{
    public BeforeAndAfterFileLocationCommand(
        string trackingDirectory,
        OrderedDictionary<string, string> tags,
        IEnumerable<string> providedBeforeTrackingFiles,
        OrderedDictionary<string, string> args)
    {
        TrackingDirectory = trackingDirectory;
        Tags = tags;
        ProvidedBeforeTrackingFiles = providedBeforeTrackingFiles;
        Args = args;
    }

    public string TrackingDirectory { get; set; }
    public OrderedDictionary<string, string> Tags { get; }
    public IEnumerable<string> ProvidedBeforeTrackingFiles { get; }
    public OrderedDictionary<string, string> Args { get; }
}