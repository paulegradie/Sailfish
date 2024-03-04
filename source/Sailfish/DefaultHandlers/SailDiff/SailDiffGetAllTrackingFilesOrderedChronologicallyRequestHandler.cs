using MediatR;
using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Requests;
using Sailfish.Extensions.Types;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.DefaultHandlers.SailDiff;

internal class SailDiffGetAllTrackingFilesOrderedChronologicallyRequestHandler(
    IRunSettings runSettings,
    ITrackingFileDirectoryReader trackingFileDirectoryReader,
    ITrackingFileParser trackingFileParser) : IRequestHandler<GetAllTrackingDataOrderedChronologicallyRequest,
    GetAllTrackingDataOrderedChronologicallyResponse>
{
    private readonly IRunSettings runSettings = runSettings;
    private readonly ITrackingFileDirectoryReader trackingFileDirectoryReader = trackingFileDirectoryReader;
    private readonly ITrackingFileParser trackingFileParser = trackingFileParser;

    public async Task<GetAllTrackingDataOrderedChronologicallyResponse> Handle(
        GetAllTrackingDataOrderedChronologicallyRequest request,
        CancellationToken cancellationToken)
    {
        var trackingFiles = trackingFileDirectoryReader
            .FindTrackingFilesInDirectoryOrderedByLastModified(runSettings.GetRunSettingsTrackingDirectoryPath(), request.Ascending);

        var data = new TrackingFileDataList();
        await trackingFileParser.TryParseMany(trackingFiles, data, cancellationToken);

        return new GetAllTrackingDataOrderedChronologicallyResponse(data);
    }
}