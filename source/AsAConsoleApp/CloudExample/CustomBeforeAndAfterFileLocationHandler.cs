using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Public.Commands;

namespace AsAConsoleApp.CloudExample;

public class CustomBeforeAndAfterFileLocationHandler : IRequestHandler<BeforeAndAfterFileLocationCommand, BeforeAndAfterFileLocationResponse>
{
    public async Task<BeforeAndAfterFileLocationResponse> Handle(BeforeAndAfterFileLocationCommand request, CancellationToken cancellationToken)
    {

        await Task.CompletedTask;
        return new BeforeAndAfterFileLocationResponse("", "");
    }
}