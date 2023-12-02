using MediatR;
using Sailfish.Contracts.Public.Requests;

namespace PerformanceTestingUserInvokedConsoleApp.CustomHandlerOverrideExamples;

public class CustomBeforeAndAfterFileLocationHandler : IRequestHandler<BeforeAndAfterFileLocationRequest, BeforeAndAfterFileLocationResponse>
{
    public async Task<BeforeAndAfterFileLocationResponse> Handle(BeforeAndAfterFileLocationRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return new BeforeAndAfterFileLocationResponse(Enumerable.Empty<string>(), Enumerable.Empty<string>());
    }
}