using MediatR;
using System.Collections.Generic;

namespace Sailfish.Contracts.Public.Requests;

public class ReadInBeforeAndAfterDataRequest(IEnumerable<string> beforeFilePaths, IEnumerable<string> afterFilePaths) : IRequest<ReadInBeforeAndAfterDataResponse>
{
    public IEnumerable<string> BeforeFilePaths { get; } = beforeFilePaths;
    public IEnumerable<string> AfterFilePaths { get; } = afterFilePaths;
}