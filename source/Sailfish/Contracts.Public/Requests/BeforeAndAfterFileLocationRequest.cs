using System.Collections.Generic;
using MediatR;

namespace Sailfish.Contracts.Public.Requests;

public class BeforeAndAfterFileLocationRequest(IEnumerable<string> providedBeforeTrackingFiles) : IRequest<BeforeAndAfterFileLocationResponse>
{
    public IEnumerable<string> ProvidedBeforeTrackingFiles { get; } = providedBeforeTrackingFiles;
}