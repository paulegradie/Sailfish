using System.Collections.Generic;
using MediatR;

namespace Sailfish.Contracts.Public.Requests;

public record BeforeAndAfterFileLocationRequest(
    IEnumerable<string> ProvidedBeforeTrackingFiles)
    : IRequest<BeforeAndAfterFileLocationResponse>;