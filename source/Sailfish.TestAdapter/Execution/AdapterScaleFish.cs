using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Requests;
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
    private readonly IMediator _mediator;
    private readonly IRunSettings _runSettings;

    public AdapterScaleFish(
        IMediator mediator,
        IRunSettings runSettings,
        IComplexityComputer complexityComputer,
        IMarkdownTableConverter markdownTableConverter,
        ILogger logger)
    {
        _mediator = mediator;
        _runSettings = runSettings;
        _complexityComputer = complexityComputer;
        _markdownTableConverter = markdownTableConverter;
        _logger = logger;
    }

    public async Task Analyze(CancellationToken cancellationToken)
    {
        if (!_runSettings.RunScaleFish) return;

        var response = await _mediator.Send(new GetLatestExecutionSummaryRequest(), cancellationToken);
        var executionSummaries = response.LatestExecutionSummaries;
        if (!executionSummaries.Any()) return;

        try
        {
            var complexityResults = _complexityComputer.AnalyzeComplexity(executionSummaries).ToList();
            if (!complexityResults.Any()) return;

            var complexityMarkdown = _markdownTableConverter.ConvertScaleFishResultToMarkdown(complexityResults);
            _logger.Log(LogLevel.Information, complexityMarkdown);
            await _mediator.Publish(new ScaleFishAnalysisCompleteNotification(complexityMarkdown, complexityResults), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, ex.Message);
        }
    }
}