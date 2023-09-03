using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff;
using Sailfish.Contracts.Private;
using Sailfish.Contracts.Serialization.V1;
using Sailfish.Exceptions;
using Sailfish.Execution;

namespace Sailfish.DefaultHandlers;

internal class SailfishGetAllTrackingFilesOrderedChronologicallyRequestHandler : IRequestHandler<SailfishGetAllTrackingDataOrderedChronologicallyRequest,
    SailfishGetAllTrackingDataOrderedChronologicallyResponse>
{
    private readonly ITrackingFileDirectoryReader trackingFileDirectoryReader;
    private readonly ITrackingFileParser trackingFileParser;

    public SailfishGetAllTrackingFilesOrderedChronologicallyRequestHandler(ITrackingFileDirectoryReader trackingFileDirectoryReader, ITrackingFileParser trackingFileParser)
    {
        this.trackingFileDirectoryReader = trackingFileDirectoryReader;
        this.trackingFileParser = trackingFileParser;
    }

    public async Task<SailfishGetAllTrackingDataOrderedChronologicallyResponse> Handle(
        SailfishGetAllTrackingDataOrderedChronologicallyRequest request,
        CancellationToken cancellationToken)
    {
        var trackingFiles = trackingFileDirectoryReader.FindTrackingFilesInDirectoryOrderedByLastModified(request.TrackingDirectory, ascending: request.Ascending);
        var data = new List<List<IExecutionSummary>>();

        if (!await trackingFileParser.TryParse(trackingFiles, data, cancellationToken))
        {
            throw new SailfishException(
                $"Failed to deserialize data into {nameof(PerformanceRunResultTrackingFormatV1)}. Please remove any non v1 data from your tracking directory.");
        }

        return new SailfishGetAllTrackingDataOrderedChronologicallyResponse(data);
    }
}