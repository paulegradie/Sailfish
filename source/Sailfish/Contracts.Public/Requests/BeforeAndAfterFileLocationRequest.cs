using MediatR;
using System.Collections.Generic;

namespace Sailfish.Contracts.Public.Requests;

public class BeforeAndAfterFileLocationRequest(IEnumerable<string> providedBeforeTrackingFiles) : IRequest<BeforeAndAfterFileLocationResponse>
{
    public IEnumerable<string> ProvidedBeforeTrackingFiles { get; } = providedBeforeTrackingFiles;
}