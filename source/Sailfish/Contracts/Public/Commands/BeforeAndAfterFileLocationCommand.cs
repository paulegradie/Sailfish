using Accord.Collections;
using MediatR;

namespace Sailfish.Contracts.Public.Commands;

public class BeforeAndAfterFileLocationCommand : IRequest<BeforeAndAfterFileLocationResponse>
{
    public BeforeAndAfterFileLocationCommand(string defaultDirectory, OrderedDictionary<string, string> tags, string beforeTarget, OrderedDictionary<string, string> args)
    {
        DefaultDirectory = defaultDirectory;
        Tags = tags;
        BeforeTarget = beforeTarget;
        Args = args;
    }

    public string DefaultDirectory { get; set; }
    public OrderedDictionary<string, string> Tags { get; }
    public string BeforeTarget { get; }
    public OrderedDictionary<string, string> Args { get; }
}