using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Contracts.Public.Requests;
using Sailfish.Execution;
using Sailfish.Extensions.Types;

namespace Sailfish.DefaultHandlers;

internal class SailfishGetLatestExecutionSummariesHandler : IRequestHandler<SailfishGetLatestExecutionSummaryRequest, SailfishGetLatestExecutionSummaryResponse>
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

    public async Task<SailfishGetLatestExecutionSummaryResponse> Handle(SailfishGetLatestExecutionSummaryRequest request, CancellationToken cancellationToken)
    {
        var trackingFiles = trackingFileDirectoryReader.FindTrackingFilesInDirectoryOrderedByLastModified(runSettings.GetRunSettingsTrackingDirectoryPath(), ascending: false);
        if (trackingFiles.Count == 0) return new SailfishGetLatestExecutionSummaryResponse(new List<IClassExecutionSummary>());

        var executionSummaries = new TrackingFileDataList();
        if (!await trackingFileParser.TryParse(trackingFiles.First(), executionSummaries, cancellationToken))
        {
            return new SailfishGetLatestExecutionSummaryResponse(new List<IClassExecutionSummary>());
        }

        return trackingFiles.Count == 0
            ? new SailfishGetLatestExecutionSummaryResponse(new List<IClassExecutionSummary>())
            : new SailfishGetLatestExecutionSummaryResponse(executionSummaries.First());
    }
}