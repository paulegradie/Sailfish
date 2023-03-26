using System.Collections.Generic;

using MediatR;
using Sailfish.Extensions.Types;

namespace Sailfish.Contracts.Public.Commands;

public class ReadInBeforeAndAfterDataCommand : IRequest<ReadInBeforeAndAfterDataResponse>
{
    public IEnumerable<string> BeforeFilePaths { get; }
    public IEnumerable<string> AfterFilePaths { get; }
    public OrderedDictionary Tags { get; }
    public OrderedDictionary Args { get; }

    public ReadInBeforeAndAfterDataCommand(
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