using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Analysis.SailDiff;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Exceptions;

namespace Sailfish.DefaultHandlers;

internal class SailfishBeforeAndAfterFileLocationHandler : IRequestHandler<BeforeAndAfterFileLocationCommand, BeforeAndAfterFileLocationResponse>
{
    private readonly ITrackingFileDirectoryReader trackingFileDirectoryReader;

    public SailfishBeforeAndAfterFileLocationHandler(ITrackingFileDirectoryReader trackingFileDirectoryReader)
    {
        this.trackingFileDirectoryReader = trackingFileDirectoryReader;
    }

    public async Task<BeforeAndAfterFileLocationResponse> Handle(BeforeAndAfterFileLocationCommand request,
        CancellationToken cancellationToken)
    {
        await Task.Yield();
        var trackingFiles = trackingFileDirectoryReader.FindTrackingFilesInDirectoryOrderedByLastModified(request.TrackingDirectory, ascending: false);
        if (trackingFiles.Count == 0)
        {
            return new BeforeAndAfterFileLocationResponse(new List<string>(), new List<string>());
        }

        if (request.ProvidedBeforeTrackingFiles.Any())
        {
            var filesFound = request.ProvidedBeforeTrackingFiles.Select(file => (file, File.Exists(file))).ToList();
            if (!filesFound.Select(x => x.Item2).All(x => x))
            {
                var missingFiles = string.Join("\n - ", filesFound.Where(x => x.Item2 == false).Select(x => x.file));
                throw new SailfishException(
                    $"Not all {nameof(BeforeAndAfterFileLocationCommand.ProvidedBeforeTrackingFiles)} were found. Missing: {missingFiles}");
            }

            return new BeforeAndAfterFileLocationResponse(filesFound.Select(x => x.file), new List<string> { trackingFiles.First() });
        }


        return trackingFiles.Count < 2
            ? new BeforeAndAfterFileLocationResponse(new List<string>(), new List<string>())
            : new BeforeAndAfterFileLocationResponse(new List<string> { trackingFiles[1] },
                new List<string> { trackingFiles[0] });
    }
}