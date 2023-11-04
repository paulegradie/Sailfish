using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Contracts.Public.Requests;

namespace PerformanceTestingConsoleApp.CustomHandlerOverrideExamples;

public class CustomBeforeAndAfterFileLocationHandler : IRequestHandler<BeforeAndAfterFileLocationRequest, BeforeAndAfterFileLocationResponse>
{
    public async Task<BeforeAndAfterFileLocationResponse> Handle(BeforeAndAfterFileLocationRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return new BeforeAndAfterFileLocationResponse(Enumerable.Empty<string>(), Enumerable.Empty<string>());
    }
}