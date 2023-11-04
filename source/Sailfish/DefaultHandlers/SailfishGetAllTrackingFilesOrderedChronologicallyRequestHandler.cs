using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff;
using Sailfish.Contracts.Private;
using Sailfish.Contracts.Serialization.V1;
using Sailfish.Exceptions;
using Sailfish.Extensions.Types;
using Serilog;

namespace Sailfish.DefaultHandlers;

internal class SailfishGetAllTrackingFilesOrderedChronologicallyRequestHandler : IRequestHandler<SailfishGetAllTrackingDataOrderedChronologicallyRequest,
    SailfishGetAllTrackingDataOrderedChronologicallyResponse>
{
    private readonly IRunSettings runSettings;
    private readonly ITrackingFileDirectoryReader trackingFileDirectoryReader;
    private readonly ITrackingFileParser trackingFileParser;
    private readonly ILogger logger;

    public SailfishGetAllTrackingFilesOrderedChronologicallyRequestHandler(
        IRunSettings runSettings,
        ITrackingFileDirectoryReader trackingFileDirectoryReader,
        ITrackingFileParser trackingFileParser,
        ILogger logger)
    {
        this.runSettings = runSettings;
        this.trackingFileDirectoryReader = trackingFileDirectoryReader;
        this.trackingFileParser = trackingFileParser;
        this.logger = logger;
    }

    public async Task<SailfishGetAllTrackingDataOrderedChronologicallyResponse> Handle(
        SailfishGetAllTrackingDataOrderedChronologicallyRequest request,
        CancellationToken cancellationToken)
    {
        var trackingFiles =
            trackingFileDirectoryReader.FindTrackingFilesInDirectoryOrderedByLastModified(runSettings.GetRunSettingsTrackingDirectoryPath(), ascending: request.Ascending);
        var data = new TrackingFileDataList();

        await trackingFileParser.TryParse(trackingFiles, data, cancellationToken);

        return new SailfishGetAllTrackingDataOrderedChronologicallyResponse(data);
    }
}