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

internal class ScaleFish(
    IMediator mediator,
    IRunSettings runSettings,
    IComplexityComputer complexityComputer,
    IMarkdownTableConverter markdownTableConverter,
    IConsoleWriter consoleWriter) : IScaleFish, IScaleFishInternal
{
    private readonly IComplexityComputer _complexityComputer = complexityComputer;
    private readonly IConsoleWriter _consoleWriter = consoleWriter;
    private readonly IMarkdownTableConverter _markdownTableConverter = markdownTableConverter;
    private readonly IMediator _mediator = mediator;
    private readonly IRunSettings _runSettings = runSettings;

    public void Analyze(ClassExecutionSummaryTrackingFormat summaryTrackingFormat)
    {
        throw new NotImplementedException();
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
            var complexityMarkdown = _markdownTableConverter.ConvertScaleFishResultToMarkdown(complexityResults);
            _consoleWriter.WriteString(complexityMarkdown);

            await _mediator.Publish(new ScaleFishAnalysisCompleteNotification(complexityMarkdown, complexityResults), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _consoleWriter.WriteString(ex.Message);
        }
    }
}