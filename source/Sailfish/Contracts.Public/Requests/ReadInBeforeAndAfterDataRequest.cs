using System.Collections.Generic;
using MediatR;

namespace Sailfish.Contracts.Public.Requests;

public record ReadInBeforeAndAfterDataRequest(
    IEnumerable<string> BeforeFilePaths,
    IEnumerable<string> AfterFilePaths)
    : IRequest<ReadInBeforeAndAfterDataResponse>;