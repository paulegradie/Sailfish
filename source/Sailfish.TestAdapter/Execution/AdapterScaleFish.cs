using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Mediation;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Requests;
using Sailfish.Execution;
using Sailfish.Logging;
using Sailfish.Presentation;

namespace Sailfish.TestAdapter.Execution;

internal interface IAdapterScaleFish : IScaleFishInternal
{
}

internal class AdapterScaleFish : IAdapterScaleFish
{
    private readonly IComplexityComputer _complexityComputer;
    private readonly ILogger _logger;
    private readonly IMarkdownTableConverter _markdownTableConverter;
    private readonly IPublisher _publisher;
    private readonly ISender _sender;
    private readonly IRunSettings _runSettings;

    public AdapterScaleFish(
        IPublisher publisher,
        ISender sender,
        IRunSettings runSettings,
        IComplexityComputer complexityComputer,
        IMarkdownTableConverter markdownTableConverter,
        ILogger logger)
    {
        _publisher = publisher;
        _sender = sender;
        _runSettings = runSettings;
        _complexityComputer = complexityComputer;
        _markdownTableConverter = markdownTableConverter;
        _logger = logger;
    }

    public async Task Analyze(CancellationToken cancellationToken)
    {
        if (!_runSettings.RunScaleFish) return;

        var response = await _sender.Send(new GetLatestExecutionSummaryRequest(), cancellationToken);
        await AnalyzeCore(response.LatestExecutionSummaries.ToList(), cancellationToken);
    }

    // Decoupled entry point — analyze the current run's in-memory summaries directly (no tracking-file
    // retrieval, no baseline dependency, no Type.GetType round-trip). See IScaleFishInternal.
    public async Task Analyze(IEnumerable<IClassExecutionSummary> executionSummaries, CancellationToken cancellationToken)
    {
        if (!_runSettings.RunScaleFish) return;
        await AnalyzeCore(executionSummaries.ToList(), cancellationToken);
    }

    private async Task AnalyzeCore(List<IClassExecutionSummary> executionSummaries, CancellationToken cancellationToken)
    {
        if (!executionSummaries.Any()) return;

        try
        {
            var complexityResults = _complexityComputer.AnalyzeComplexity(executionSummaries).ToList();
            if (!complexityResults.Any()) return;

            var complexityMarkdown = _markdownTableConverter.ConvertScaleFishResultToMarkdown(complexityResults);
            _logger.Log(LogLevel.Information, complexityMarkdown);
            await _publisher.Publish(new ScaleFishAnalysisCompleteNotification(complexityMarkdown, complexityResults), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, ex.Message);
        }
    }
}