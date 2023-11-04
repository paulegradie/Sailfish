using System.Collections.Generic;
using MediatR;
using Sailfish.Extensions.Types;

namespace Sailfish.Contracts.Public.Requests;

public class BeforeAndAfterFileLocationRequest : IRequest<BeforeAndAfterFileLocationResponse>
{
    public BeforeAndAfterFileLocationRequest(
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