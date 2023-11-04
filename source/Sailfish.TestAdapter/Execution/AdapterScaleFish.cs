using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Contracts.Public.Requests;
using Sailfish.Presentation;

namespace Sailfish.TestAdapter.Execution;

internal class AdapterScaleFish : IAdapterScaleFish
{
    private readonly IMediator mediator;
    private readonly IRunSettings runSettings;
    private readonly IComplexityComputer complexityComputer;
    private readonly IMarkdownTableConverter markdownTableConverter;
    private readonly IAdapterConsoleWriter consoleWriter;

    public AdapterScaleFish(
        IMediator mediator,
        IRunSettings runSettings,
        IComplexityComputer complexityComputer,
        IMarkdownTableConverter markdownTableConverter,
        IAdapterConsoleWriter consoleWriter)
    {
        this.mediator = mediator;
        this.runSettings = runSettings;
        this.complexityComputer = complexityComputer;
        this.markdownTableConverter = markdownTableConverter;
        this.consoleWriter = consoleWriter;
    }

    public async Task Analyze(DateTime timeStamp, CancellationToken cancellationToken)
    {
        if (!runSettings.RunScalefish) return;

        var response = await mediator.Send(new SailfishGetLatestExecutionSummaryRequest(), cancellationToken);
        var executionSummaries = response.LatestExecutionSummaries;
        if (!executionSummaries.Any()) return;

        try
        {
            var complexityResults = complexityComputer.AnalyzeComplexity(executionSummaries).ToList();
            if (!complexityResults.Any()) return;

            var complexityMarkdown = markdownTableConverter.ConvertScaleFishResultToMarkdown(complexityResults);
            consoleWriter.WriteString(complexityMarkdown);
            await mediator.Publish(new WriteCurrentScalefishResultNotification(complexityMarkdown, timeStamp), cancellationToken).ConfigureAwait(false);
            await mediator.Publish(new WriteCurrentScalefishResultModelsNotification(complexityResults, timeStamp), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            consoleWriter.WriteString(ex.Message);
        }
    }
}