using System.Collections.Generic;
using MediatR;

namespace Sailfish.Contracts.Public.Requests;

public class BeforeAndAfterFileLocationRequest : IRequest<BeforeAndAfterFileLocationResponse>
{
    public BeforeAndAfterFileLocationRequest(IEnumerable<string> providedBeforeTrackingFiles)
    {
        ProvidedBeforeTrackingFiles = providedBeforeTrackingFiles;
    }

    public IEnumerable<string> ProvidedBeforeTrackingFiles { get; }
}