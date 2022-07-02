using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Commands;

namespace AsAConsoleApp.CloudExample;

public class CustomBeforeAndAfterFileLocationHandler : IRequestHandler<BeforeAndAfterFileLocationCommand, BeforeAndAfterFileLocationResponse>
{
    public Task<BeforeAndAfterFileLocationResponse> Handle(BeforeAndAfterFileLocationCommand request, CancellationToken cancellationToken)
    {
        c
        
        
        
        return new BeforeAndAfterFileLocationResponse();
    }
}