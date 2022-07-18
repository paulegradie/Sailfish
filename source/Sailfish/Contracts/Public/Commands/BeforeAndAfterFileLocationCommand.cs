using Accord.Collections;
using MediatR;

namespace Sailfish.Contracts.Public.Commands;

public class BeforeAndAfterFileLocationCommand : IRequest<BeforeAndAfterFileLocationResponse>
{
    public BeforeAndAfterFileLocationCommand(string defaultDirectory, OrderedDictionary<string, string> tags, string beforeTarget)
    {
        DefaultDirectory = defaultDirectory;
        this.Tags = tags;
        BeforeTarget = beforeTarget;
    }

    public string DefaultDirectory { get; set; }
    public OrderedDictionary<string, string> Tags { get; }
    public string BeforeTarget { get; }
}