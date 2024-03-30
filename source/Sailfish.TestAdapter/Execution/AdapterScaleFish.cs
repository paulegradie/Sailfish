using MediatR;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Requests;
using Sailfish.Logging;
using Sailfish.Presentation;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.TestAdapter.Execution;

internal interface IAdapterScaleFish : IScaleFishInternal
{
}

internal class AdapterScaleFish : IAdapterScaleFish
{
    private readonly IComplexityComputer complexityComputer;
    private readonly ILogger logger;
    private readonly IMarkdownTableConverter markdownTableConverter;
    private readonly IMediator mediator;
    private readonly IRunSettings runSettings;

    public AdapterScaleFish(
        IMediator mediator,
        IRunSettings runSettings,
        IComplexityComputer complexityComputer,
        IMarkdownTableConverter markdownTableConverter,
        ILogger logger)
    {
        this.mediator = mediator;
        this.runSettings = runSettings;
        this.complexityComputer = complexityComputer;
        this.markdownTableConverter = markdownTableConverter;
        this.logger = logger;
    }

    public async Task Analyze(CancellationToken cancellationToken)
    {
        if (!runSettings.RunScaleFish) return;

        var response = await mediator.Send(new GetLatestExecutionSummaryRequest(), cancellationToken);
        var executionSummaries = response.LatestExecutionSummaries;
        if (!executionSummaries.Any()) return;

        try
        {
            var complexityResults = complexityComputer.AnalyzeComplexity(executionSummaries).ToList();
            if (!complexityResults.Any()) return;

            var complexityMarkdown = markdownTableConverter.ConvertScaleFishResultToMarkdown(complexityResults);
            logger.Log(LogLevel.Information, complexityMarkdown);
            await mediator.Publish(new ScaleFishAnalysisCompleteNotification(complexityMarkdown, complexityResults), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.Log(LogLevel.Error, ex.Message);
        }
    }
}