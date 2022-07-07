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

    public async Task<BeforeAndAfterFileLocationResponse> Handle(BeforeAndAfterFileLocationCommand request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        var trackingFiles = trackingFileFinder.GetBeforeAndAfterTrackingFiles(request.DefaultDirectory, request.Tags);
        return new BeforeAndAfterFileLocationResponse(trackingFiles.BeforeFilePath, trackingFiles.AfterFilePath);
    }
}