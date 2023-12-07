using MediatR;
using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Requests;
using Sailfish.Execution;
using Sailfish.Extensions.Types;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.DefaultHandlers.SailDiff;

internal class SailDiffGetLatestExecutionSummariesHandler(ITrackingFileDirectoryReader trackingFileDirectoryReader, ITrackingFileParser trackingFileParser, IRunSettings runSettings) : IRequestHandler<GetLatestExecutionSummaryRequest, GetLatestExecutionSummaryResponse>
{
    private readonly IRunSettings runSettings = runSettings;
    private readonly ITrackingFileDirectoryReader trackingFileDirectoryReader = trackingFileDirectoryReader;
    private readonly ITrackingFileParser trackingFileParser = trackingFileParser;

    public async Task<GetLatestExecutionSummaryResponse> Handle(GetLatestExecutionSummaryRequest request, CancellationToken cancellationToken)
    {
        var trackingFiles = trackingFileDirectoryReader.FindTrackingFilesInDirectoryOrderedByLastModified(runSettings.GetRunSettingsTrackingDirectoryPath(), false);
        if (trackingFiles.Count == 0) return new GetLatestExecutionSummaryResponse(new List<IClassExecutionSummary>());

        var executionSummaries = new TrackingFileDataList();
        if (!await trackingFileParser.TryParse(trackingFiles.First(), executionSummaries, cancellationToken))
            return new GetLatestExecutionSummaryResponse(new List<IClassExecutionSummary>());

        return trackingFiles.Count == 0
            ? new GetLatestExecutionSummaryResponse(new List<IClassExecutionSummary>())
            : new GetLatestExecutionSummaryResponse(executionSummaries.First());
    }
}