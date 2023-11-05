using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff;
using Sailfish.Contracts.Public.Requests;
using Sailfish.Execution;
using Sailfish.Extensions.Types;

namespace Sailfish.DefaultHandlers.SailDiff;

internal class SailfishGetLatestExecutionSummariesHandler : IRequestHandler<GetLatestExecutionSummaryRequest, GetLatestExecutionSummaryResponse>
{
    private readonly ITrackingFileDirectoryReader trackingFileDirectoryReader;
    private readonly ITrackingFileParser trackingFileParser;
    private readonly IRunSettings runSettings;

    public SailfishGetLatestExecutionSummariesHandler(ITrackingFileDirectoryReader trackingFileDirectoryReader, ITrackingFileParser trackingFileParser, IRunSettings runSettings)
    {
        this.trackingFileDirectoryReader = trackingFileDirectoryReader;
        this.trackingFileParser = trackingFileParser;
        this.runSettings = runSettings;
    }

    public async Task<GetLatestExecutionSummaryResponse> Handle(GetLatestExecutionSummaryRequest request, CancellationToken cancellationToken)
    {
        var trackingFiles = trackingFileDirectoryReader.FindTrackingFilesInDirectoryOrderedByLastModified(runSettings.GetRunSettingsTrackingDirectoryPath(), ascending: false);
        if (trackingFiles.Count == 0) return new GetLatestExecutionSummaryResponse(new List<IClassExecutionSummary>());

        var executionSummaries = new TrackingFileDataList();
        if (!await trackingFileParser.TryParse(trackingFiles.First(), executionSummaries, cancellationToken))
        {
            return new GetLatestExecutionSummaryResponse(new List<IClassExecutionSummary>());
        }

        return trackingFiles.Count == 0
            ? new GetLatestExecutionSummaryResponse(new List<IClassExecutionSummary>())
            : new GetLatestExecutionSummaryResponse(executionSummaries.First());
    }
}