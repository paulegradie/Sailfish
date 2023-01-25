﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Public.Commands;

namespace PerformanceTestingConsoleApp.CustomHandlerOverrideExamples;

public class CustomBeforeAndAfterFileLocationHandler : IRequestHandler<BeforeAndAfterFileLocationCommand, BeforeAndAfterFileLocationResponse>
{
    public async Task<BeforeAndAfterFileLocationResponse> Handle(BeforeAndAfterFileLocationCommand request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return new BeforeAndAfterFileLocationResponse(Enumerable.Empty<string>(), Enumerable.Empty<string>());
    }
}