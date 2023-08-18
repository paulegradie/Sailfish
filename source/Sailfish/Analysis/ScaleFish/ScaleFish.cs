using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Presentation;
using Sailfish.Presentation.Console;

namespace Sailfish.Analysis.Scalefish;

public class ScaleFish : IScaleFish
{
    private readonly IMarkdownTableConverter markdownTableConverter;
    private readonly IConsoleWriter consoleWriter;
    private readonly IMediator mediator;
    private readonly IComplexityComputer complexityComputer;

    public ScaleFish(IMediator mediator, IComplexityComputer complexityComputer, IMarkdownTableConverter markdownTableConverter, IConsoleWriter consoleWriter)
    {
        this.mediator = mediator;
        this.complexityComputer = complexityComputer;
        this.markdownTableConverter = markdownTableConverter;
        this.consoleWriter = consoleWriter;
    }

    public async Task Analyze(
        DateTime timeStamp,
        IRunSettings runSettings,
        string trackingDir,
        CancellationToken cancellationToken)
    {
        if (!runSettings.RunScalefish) return;

        var response = await mediator.Send(new SailfishGetLatestExecutionSummariesCommand(trackingDir, runSettings.Tags, runSettings.Args), cancellationToken);
        var executionSummaries = response.LatestExecutionSummaries;
        if (!executionSummaries.Any()) return;

        try
        {
            var complexityResults = complexityComputer.AnalyzeComplexity(executionSummaries);
            var complexityMarkdown = markdownTableConverter.ConvertScaleFishResultToMarkdown(complexityResults);
            consoleWriter.WriteString(complexityMarkdown);

            await mediator.Publish(new WriteCurrentScalefishResultCommand(
                        complexityMarkdown,
                        runSettings.LocalOutputDirectory ?? DefaultFileSettings.DefaultOutputDirectory,
                        timeStamp,
                        runSettings.Tags,
                        runSettings.Args),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            consoleWriter.WriteString(ex.Message);
        }
    }
}