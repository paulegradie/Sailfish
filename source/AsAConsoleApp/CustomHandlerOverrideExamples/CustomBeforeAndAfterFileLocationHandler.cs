using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Public.Commands;

namespace AsAConsoleApp.CustomHandlerOverrideExamples;

public class CustomBeforeAndAfterFileLocationHandler : IRequestHandler<BeforeAndAfterFileLocationCommand, BeforeAndAfterFileLocationResponse>
{
    public async Task<BeforeAndAfterFileLocationResponse> Handle(BeforeAndAfterFileLocationCommand request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return new BeforeAndAfterFileLocationResponse(new List<string>() { string.Empty }, new List<string>() { string.Empty });
    }
}