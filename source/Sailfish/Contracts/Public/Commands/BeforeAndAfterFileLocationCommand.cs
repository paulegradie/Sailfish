using System.Collections.Generic;

using MediatR;
using Sailfish.Extensions.Types;

namespace Sailfish.Contracts.Public.Commands;

public class BeforeAndAfterFileLocationCommand : IRequest<BeforeAndAfterFileLocationResponse>
{
    public BeforeAndAfterFileLocationCommand(
        string trackingDirectory,
        OrderedDictionary tags,
        IEnumerable<string> providedBeforeTrackingFiles,
        OrderedDictionary args)
    {
        TrackingDirectory = trackingDirectory;
        Tags = tags;
        ProvidedBeforeTrackingFiles = providedBeforeTrackingFiles;
        Args = args;
    }

    public string TrackingDirectory { get; }
    public OrderedDictionary Tags { get; }
    public IEnumerable<string> ProvidedBeforeTrackingFiles { get; }
    public OrderedDictionary Args { get; }
}