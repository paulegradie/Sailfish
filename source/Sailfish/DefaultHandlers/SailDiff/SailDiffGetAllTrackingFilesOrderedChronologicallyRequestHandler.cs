using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Requests;
using Sailfish.Extensions.Types;

namespace Sailfish.DefaultHandlers.SailDiff;

internal class SailDiffGetAllTrackingFilesOrderedChronologicallyRequestHandler : IRequestHandler<GetAllTrackingDataOrderedChronologicallyRequest,
    GetAllTrackingDataOrderedChronologicallyResponse>
{
    private readonly IRunSettings runSettings;
    private readonly ITrackingFileDirectoryReader trackingFileDirectoryReader;
    private readonly ITrackingFileParser trackingFileParser;

    public SailDiffGetAllTrackingFilesOrderedChronologicallyRequestHandler(
        IRunSettings runSettings,
        ITrackingFileDirectoryReader trackingFileDirectoryReader,
        ITrackingFileParser trackingFileParser)
    {
        this.runSettings = runSettings;
        this.trackingFileDirectoryReader = trackingFileDirectoryReader;
        this.trackingFileParser = trackingFileParser;
    }

    public async Task<GetAllTrackingDataOrderedChronologicallyResponse> Handle(
        GetAllTrackingDataOrderedChronologicallyRequest request,
        CancellationToken cancellationToken)
    {
        var trackingFiles = trackingFileDirectoryReader
            .FindTrackingFilesInDirectoryOrderedByLastModified(runSettings.GetRunSettingsTrackingDirectoryPath(), ascending: request.Ascending);

        var data = new TrackingFileDataList();
        await trackingFileParser.TryParse(trackingFiles, data, cancellationToken);

        return new GetAllTrackingDataOrderedChronologicallyResponse(data);
    }
}