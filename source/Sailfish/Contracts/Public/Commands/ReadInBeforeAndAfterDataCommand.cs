using System.Collections.Generic;
using System.Collections.Specialized;
using MediatR;

namespace Sailfish.Contracts.Public.Commands;

public class ReadInBeforeAndAfterDataCommand : IRequest<ReadInBeforeAndAfterDataResponse>
{
    public IEnumerable<string> BeforeFilePaths { get; set; }
    public IEnumerable<string> AfterFilePaths { get; set; }
    public OrderedDictionary Tags { get; set; }
    public OrderedDictionary Args { get; set; }

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