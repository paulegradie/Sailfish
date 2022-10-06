using Accord.Collections;
using MediatR;

namespace Sailfish.Contracts.Public.Commands;

public class ReadInBeforeAndAfterDataCommand : IRequest<ReadInBeforeAndAfterDataResponse>
{
    public string BeforeFilePath { get; set; }
    public string AfterFilePath { get; set; }
    public OrderedDictionary<string, string> Tags { get; set; }
    public OrderedDictionary<string, string> Args { get; set; }
    public string BeforeTarget { get; set; }

    public ReadInBeforeAndAfterDataCommand(
        string beforeFilePath,
        string afterFilePath,
        string beforeTarget,
        OrderedDictionary<string, string> tags,
        OrderedDictionary<string, string> args)
    {
        BeforeFilePath = beforeFilePath;
        AfterFilePath = afterFilePath;
        BeforeTarget = beforeTarget;
        Tags = tags;
        Args = args;
    }
}