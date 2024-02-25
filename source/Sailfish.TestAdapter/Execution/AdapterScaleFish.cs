using MediatR;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Requests;
using Sailfish.Presentation;
using Sailfish.Presentation.Console;
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
    private readonly IConsoleWriter consoleWriter;
    private readonly IMarkdownTableConverter markdownTableConverter;
    private readonly IMediator mediator;
    private readonly IRunSettings runSettings;

    public AdapterScaleFish(
        IMediator mediator,
        IRunSettings runSettings,
        IComplexityComputer complexityComputer,
        IMarkdownTableConverter markdownTableConverter,
        IConsoleWriter consoleWriter)
    {
        this.mediator = mediator;
        this.runSettings = runSettings;
        this.complexityComputer = complexityComputer;
        this.markdownTableConverter = markdownTableConverter;
        this.consoleWriter = consoleWriter;
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
            consoleWriter.WriteString(complexityMarkdown);
            await mediator.Publish(new ScalefishAnalysisCompleteNotification(complexityMarkdown, complexityResults), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            consoleWriter.WriteString(ex.Message);
        }
    }
}