using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff;
using Sailfish.Contracts.Private;
using Sailfish.Contracts.Serialization.V1;
using Sailfish.Exceptions;
using Sailfish.Extensions.Types;

namespace Sailfish.DefaultHandlers;

internal class SailfishGetAllTrackingFilesOrderedChronologicallyRequestHandler : IRequestHandler<SailfishGetAllTrackingDataOrderedChronologicallyRequest,
    SailfishGetAllTrackingDataOrderedChronologicallyResponse>
{
    private readonly IRunSettings runSettings;
    private readonly ITrackingFileDirectoryReader trackingFileDirectoryReader;
    private readonly ITrackingFileParser trackingFileParser;

    public SailfishGetAllTrackingFilesOrderedChronologicallyRequestHandler(
        IRunSettings runSettings,
        ITrackingFileDirectoryReader trackingFileDirectoryReader,
        ITrackingFileParser trackingFileParser)
    {
        this.runSettings = runSettings;
        this.trackingFileDirectoryReader = trackingFileDirectoryReader;
        this.trackingFileParser = trackingFileParser;
    }

    public async Task<SailfishGetAllTrackingDataOrderedChronologicallyResponse> Handle(
        SailfishGetAllTrackingDataOrderedChronologicallyRequest request,
        CancellationToken cancellationToken)
    {
        var trackingFiles =
            trackingFileDirectoryReader.FindTrackingFilesInDirectoryOrderedByLastModified(runSettings.GetRunSettingsTrackingDirectoryPath(), ascending: request.Ascending);
        var data = new TrackingFileDataList();

        if (!await trackingFileParser.TryParse(trackingFiles, data, cancellationToken))
        {
            throw new SailfishException(
                $"Failed to deserialize data into {nameof(PerformanceRunResultTrackingFormat)}. Please remove any non v1 data from your tracking directory.");
        }

        return new SailfishGetAllTrackingDataOrderedChronologicallyResponse(data);
    }
}