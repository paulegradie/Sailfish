using MediatR;

namespace Sailfish.Contracts.Public.Commands;

public class BeforeAndAfterFileLocationCommand : IRequest<BeforeAndAfterFileLocationResponse>
{
    public BeforeAndAfterFileLocationCommand(string defaultDirectory)
    {
        DefaultDirectory = defaultDirectory;
    }

    public string DefaultDirectory { get; set; }
}