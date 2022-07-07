using Accord.Collections;
using MediatR;

namespace Sailfish.Contracts.Public.Commands;

public class BeforeAndAfterFileLocationCommand : IRequest<BeforeAndAfterFileLocationResponse>
{
    public BeforeAndAfterFileLocationCommand(string defaultDirectory, OrderedDictionary<string, string> tags)
    {
        DefaultDirectory = defaultDirectory;
        this.Tags = tags;
    }

    public string DefaultDirectory { get; set; }
    public OrderedDictionary<string, string> Tags { get; }
}