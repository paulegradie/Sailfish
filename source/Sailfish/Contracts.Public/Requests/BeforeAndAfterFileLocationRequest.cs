using System.Collections.Generic;
using MediatR;

namespace Sailfish.Contracts.Public.Requests;

public record BeforeAndAfterFileLocationRequest : IRequest<BeforeAndAfterFileLocationResponse>
{
    public BeforeAndAfterFileLocationRequest(IEnumerable<string> ProvidedBeforeTrackingFiles)
    {
        this.ProvidedBeforeTrackingFiles = ProvidedBeforeTrackingFiles;
    }

    public IEnumerable<string> ProvidedBeforeTrackingFiles { get; init; }

    public void Deconstruct(out IEnumerable<string> ProvidedBeforeTrackingFiles)
    {
        ProvidedBeforeTrackingFiles = this.ProvidedBeforeTrackingFiles;
    }
}