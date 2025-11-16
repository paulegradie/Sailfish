using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Requests;
using Sailfish.Extensions.Types;

namespace Sailfish.DefaultHandlers.SailDiff;

internal class SailDiffGetAllTrackingFilesOrderedChronologicallyRequestHandler : IRequestHandler<GetAllTrackingDataOrderedChronologicallyRequest,
    GetAllTrackingDataOrderedChronologicallyResponse>
{
    private readonly IRunSettings _runSettings;
    private readonly ITrackingFileDirectoryReader _trackingFileDirectoryReader;
    private readonly ITrackingFileParser _trackingFileParser;

    public SailDiffGetAllTrackingFilesOrderedChronologicallyRequestHandler(IRunSettings runSettings,
        ITrackingFileDirectoryReader trackingFileDirectoryReader,
        ITrackingFileParser trackingFileParser)
    {
        _runSettings = runSettings;
        _trackingFileDirectoryReader = trackingFileDirectoryReader;
        _trackingFileParser = trackingFileParser;
    }

    public async Task<GetAllTrackingDataOrderedChronologicallyResponse> Handle(
        GetAllTrackingDataOrderedChronologicallyRequest request,
        CancellationToken cancellationToken)
    {
        var trackingFiles = _trackingFileDirectoryReader
            .FindTrackingFilesInDirectoryOrderedByLastModified(_runSettings.GetRunSettingsTrackingDirectoryPath(), request.Ascending);

        var data = new TrackingFileDataList();
        await _trackingFileParser.TryParseMany(trackingFiles, data, cancellationToken);

        return new GetAllTrackingDataOrderedChronologicallyResponse(data);
    }
}