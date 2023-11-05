using System.Collections.Generic;
using MediatR;

namespace Sailfish.Contracts.Public.Requests;

public class ReadInBeforeAndAfterDataRequest : IRequest<ReadInBeforeAndAfterDataResponse>
{
    public IEnumerable<string> BeforeFilePaths { get; }
    public IEnumerable<string> AfterFilePaths { get; }

    public ReadInBeforeAndAfterDataRequest(IEnumerable<string> beforeFilePaths, IEnumerable<string> afterFilePaths)
    {
        BeforeFilePaths = beforeFilePaths;
        AfterFilePaths = afterFilePaths;
    }
}