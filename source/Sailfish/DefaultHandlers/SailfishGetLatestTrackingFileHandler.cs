using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Execution;

namespace Sailfish.DefaultHandlers;

internal class SailfishGetLatestExecutionSummariesHandler : IRequestHandler<SailfishGetLatestExecutionSummaryCommand, SailfishGetLatestExecutionSummaryResponse>
{
    private readonly ITrackingFileDirectoryReader trackingFileDirectoryReader;
    private readonly ITrackingFileParser trackingFileParser;

    public SailfishGetLatestExecutionSummariesHandler(ITrackingFileDirectoryReader trackingFileDirectoryReader, ITrackingFileParser trackingFileParser)
    {
        this.trackingFileDirectoryReader = trackingFileDirectoryReader;
        this.trackingFileParser = trackingFileParser;
    }

    public async Task<SailfishGetLatestExecutionSummaryResponse> Handle(SailfishGetLatestExecutionSummaryCommand request, CancellationToken cancellationToken)
    {
        var trackingFiles = trackingFileDirectoryReader.FindTrackingFilesInDirectoryOrderedByLastModified(request.TrackingDirectory, ascending: false);
        if (trackingFiles.Count == 0) return new SailfishGetLatestExecutionSummaryResponse(new List<IExecutionSummary>());

        var executionSummaries = new List<List<IExecutionSummary>>();
        if (!await trackingFileParser.TryParse(trackingFiles.First(), executionSummaries, cancellationToken))
        {
            return new SailfishGetLatestExecutionSummaryResponse(new List<IExecutionSummary>());
        }

        return trackingFiles.Count == 0
            ? new SailfishGetLatestExecutionSummaryResponse(new List<IExecutionSummary>())
            : new SailfishGetLatestExecutionSummaryResponse(executionSummaries.First());
    }
}