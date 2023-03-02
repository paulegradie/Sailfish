using System.Collections.Generic;
using Accord.Collections;
using MediatR;

namespace Sailfish.Contracts.Public.Commands;

public class ReadInBeforeAndAfterDataCommand : IRequest<ReadInBeforeAndAfterDataResponse>
{
    public IEnumerable<string> BeforeFilePaths { get; set; }
    public IEnumerable<string> AfterFilePaths { get; set; }
    public OrderedDictionary<string, string> Tags { get; set; }
    public OrderedDictionary<string, string> Args { get; set; }

    public ReadInBeforeAndAfterDataCommand(
        IEnumerable<string> beforeFilePaths,
        IEnumerable<string> afterFilePaths,
        OrderedDictionary<string, string> tags,
        OrderedDictionary<string, string> args)
    {
        BeforeFilePaths = beforeFilePaths;
        AfterFilePaths = afterFilePaths;
        Tags = tags;
        Args = args;
    }
}