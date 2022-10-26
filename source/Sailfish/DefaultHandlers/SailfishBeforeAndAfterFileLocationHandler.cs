using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Statistics.StatisticalAnalysis;

namespace Sailfish.DefaultHandlers;

internal class SailfishBeforeAndAfterFileLocationHandler : IRequestHandler<BeforeAndAfterFileLocationCommand, BeforeAndAfterFileLocationResponse>
{
    private readonly ITrackingFileFinder trackingFileFinder;

    public SailfishBeforeAndAfterFileLocationHandler(ITrackingFileFinder trackingFileFinder)
    {
        this.trackingFileFinder = trackingFileFinder;
    }

    public Task<BeforeAndAfterFileLocationResponse> Handle(BeforeAndAfterFileLocationCommand request, CancellationToken cancellationToken)
    {
        var trackingFiles = trackingFileFinder.GetBeforeAndAfterTrackingFiles(request.DefaultDirectory, request.BeforeTarget, request.Tags);
        return Task.FromResult(new BeforeAndAfterFileLocationResponse(
            new List<string>() { trackingFiles.BeforeFilePath }.Where(x => !string.IsNullOrEmpty(x)),
            new List<string>() { trackingFiles.AfterFilePath }.Where(x => !string.IsNullOrEmpty(x))));
    }
}