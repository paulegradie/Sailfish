using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Analysis;
using Sailfish.Contracts.Public.Commands;

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
        BeforeAndAfterTrackingFiles trackingFiles;
        if (File.Exists(request.ProvidedBeforeTrackingFiles))
        {
            trackingFiles = trackingFileFinder.GetBeforeAndAfterTrackingFiles(request.TrackingDirectory, request.ProvidedBeforeTrackingFiles, request.Tags);
        }
        else
        {
            trackingFiles = trackingFileFinder.GetBeforeAndAfterTrackingFiles(request.TrackingDirectory, request.ProvidedBeforeTrackingFiles, request.Tags);
        }

        return Task.FromResult(new BeforeAndAfterFileLocationResponse(
            new List<string>() { trackingFiles.BeforeFilePath }.Where(x => !string.IsNullOrEmpty(x)),
            new List<string>() { trackingFiles.AfterFilePath }.Where(x => !string.IsNullOrEmpty(x))));
    }
}