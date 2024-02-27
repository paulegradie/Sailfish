using MediatR;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Requests;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Sailfish.Presentation;
using Sailfish.Presentation.Console;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Analysis.ScaleFish;

public interface IScaleFish
{
    void Analyze(ClassExecutionSummaryTrackingFormat summaryTrackingFormat);
}

internal class ScaleFish(
    IMediator mediator,
    IRunSettings runSettings,
    IComplexityComputer complexityComputer,
    IMarkdownTableConverter markdownTableConverter,
    IConsoleWriter consoleWriter) : IScaleFish, IScaleFishInternal
{
    private readonly IComplexityComputer complexityComputer = complexityComputer;
    private readonly IConsoleWriter consoleWriter = consoleWriter;
    private readonly IMarkdownTableConverter markdownTableConverter = markdownTableConverter;
    private readonly IMediator mediator = mediator;
    private readonly IRunSettings runSettings = runSettings;

    public void Analyze(ClassExecutionSummaryTrackingFormat summaryTrackingFormat)
    {
        throw new NotImplementedException();
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

            await mediator.Publish(new ScaleFishAnalysisCompleteNotification(complexityMarkdown, complexityResults), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            consoleWriter.WriteString(ex.Message);
        }
    }
}