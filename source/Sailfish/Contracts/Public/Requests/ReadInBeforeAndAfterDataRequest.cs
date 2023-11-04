using System.Collections.Generic;
using MediatR;
using Sailfish.Extensions.Types;

namespace Sailfish.Contracts.Public.Requests;

public class ReadInBeforeAndAfterDataRequest : IRequest<ReadInBeforeAndAfterDataResponse>
{
    public IEnumerable<string> BeforeFilePaths { get; }
    public IEnumerable<string> AfterFilePaths { get; }
    public OrderedDictionary Tags { get; }
    public OrderedDictionary Args { get; }

    public ReadInBeforeAndAfterDataRequest(
        IEnumerable<string> beforeFilePaths,
        IEnumerable<string> afterFilePaths,
        OrderedDictionary tags,
        OrderedDictionary args)
    {
        BeforeFilePaths = beforeFilePaths;
        AfterFilePaths = afterFilePaths;
        Tags = tags;
        Args = args;
    }
}