using System.Collections.Generic;

using MediatR;
using Sailfish.Extensions.Types;

namespace Sailfish.Contracts.Public.Commands;

public class BeforeAndAfterFileLocationCommand : IRequest<BeforeAndAfterFileLocationResponse>
{
    public BeforeAndAfterFileLocationCommand(
        OrderedDictionary tags,
        IEnumerable<string> providedBeforeTrackingFiles,
        OrderedDictionary args)
    {
        Tags = tags;
        ProvidedBeforeTrackingFiles = providedBeforeTrackingFiles;
        Args = args;
    }

    public OrderedDictionary Tags { get; }
    public IEnumerable<string> ProvidedBeforeTrackingFiles { get; }
    public OrderedDictionary Args { get; }
}