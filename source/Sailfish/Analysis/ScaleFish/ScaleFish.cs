using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Requests;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Sailfish.Presentation;
using Sailfish.Presentation.Console;

namespace Sailfish.Analysis.ScaleFish;

public interface IScaleFish
{
    void Analyze(ClassExecutionSummaryTrackingFormat summaryTrackingFormat);
}

internal class ScaleFish : IScaleFish, IScaleFishInternal
{
    private readonly IMarkdownTableConverter markdownTableConverter;
    private readonly IConsoleWriter consoleWriter;
    private readonly IMediator mediator;
    private readonly IRunSettings runSettings;
    private readonly IComplexityComputer complexityComputer;

    public ScaleFish(
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
            var complexityMarkdown = markdownTableConverter.ConvertScaleFishResultToMarkdown(complexityResults);
            consoleWriter.WriteString(complexityMarkdown);

            await mediator.Publish(new ScalefishAnalysisCompleteNotification(complexityMarkdown, complexityResults), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            consoleWriter.WriteString(ex.Message);
        }
    }

    public void Analyze(ClassExecutionSummaryTrackingFormat summaryTrackingFormat)
    {
        throw new NotImplementedException();
    }
}