using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NSubstitute;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Requests;
using Sailfish.Execution;
using Sailfish.Logging;
using Sailfish.Presentation;
using Sailfish.Presentation.Console;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.ScaleFish;

public class ScaleFishDecouplingTests
{
    [Fact]
    public async Task Analyze_WithInMemorySummaries_DoesNotQueryTrackingFiles_AndPublishesCompletion()
    {
        // #296: ScaleFish is a single-run analysis. Given the current run's in-memory summaries it must NOT
        // go through the SailDiff-shared tracking-file retrieval (which has no baseline on a first run), and
        // it must publish the completion notification that Skipper hangs off — so Skipper fires on a single
        // run.
        var mediator = Substitute.For<IMediator>();
        var runSettings = Substitute.For<IRunSettings>();
        runSettings.RunScaleFish.Returns(true);
        runSettings.ScaleFishSettings.Returns(new ScaleFishSettings { EnableTrendTracking = false, EmitHtmlReport = false });

        var computer = Substitute.For<IComplexityComputer>();
        computer.AnalyzeComplexityWithMeasurements(Arg.Any<List<IClassExecutionSummary>>())
            .Returns(new ComplexityAnalysisResult(
                new List<ScalefishClassModel> { new("NS", "Cls", Enumerable.Empty<ScaleFishMethodModel>()) },
                new Dictionary<string, ComplexityMeasurement[]>()));

        var markdown = Substitute.For<IMarkdownTableConverter>();
        markdown.ConvertScaleFishResultToMarkdown(Arg.Any<IEnumerable<ScalefishClassModel>>()).Returns("md");

        var scaleFish = new Sailfish.Analysis.ScaleFish.ScaleFish(
            mediator, runSettings, computer, markdown, Substitute.For<IConsoleWriter>(), Substitute.For<ILogger>());

        var currentRun = new List<IClassExecutionSummary> { Substitute.For<IClassExecutionSummary>() };
        await scaleFish.Analyze(currentRun, CancellationToken.None);

        // Decoupled: no tracking-file retrieval.
        await mediator.DidNotReceive().Send(Arg.Any<GetLatestExecutionSummaryRequest>(), Arg.Any<CancellationToken>());
        // It analyzed exactly the in-memory summaries it was handed.
        computer.Received(1).AnalyzeComplexityWithMeasurements(Arg.Is<List<IClassExecutionSummary>>(s => s.Count == 1));
        // Skipper hangs off this notification — it must fire on a single run.
        await mediator.Received(1).Publish(Arg.Any<ScaleFishAnalysisCompleteNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ComplexityComputer_SkipsSummaryWithUnresolvedType_InsteadOfThrowingNullKey()
    {
        // #296: a summary loaded from a tracking file whose TestClass could not be resolved
        // (Type.GetType -> null) must be skipped, not crash Dictionary<Type,...>.Add with
        // ArgumentNullException("key").
        var computer = new ComplexityComputer(
            Substitute.For<IComplexityEstimator>(),
            Substitute.For<IScalefishObservationCompiler>());

        var summary = Substitute.For<IClassExecutionSummary>();
        summary.TestClass.Returns((Type)null!);

        var result = computer.AnalyzeComplexityWithMeasurements(new List<IClassExecutionSummary> { summary });

        result.Classes.ShouldBeEmpty();
    }
}
