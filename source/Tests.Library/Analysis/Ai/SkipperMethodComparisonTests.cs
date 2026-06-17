using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Sailfish.Analysis.Ai;
using Sailfish.Analysis.SailDiff;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.DefaultHandlers.Ai;
using Sailfish.Diagnostics.Environment;
using Sailfish.Results;
using Shouldly;
using Tests.Common.Builders;
using Xunit;

namespace Tests.Library.Analysis.Ai;

/// <summary>
///     Deliverable #5: a completed in-run method-vs-method ComparisonGroup must feed Skipper. Covers the handler
///     (gating + context build + runner invocation) and the verdict/orientation mapping the context builder uses.
/// </summary>
public class SkipperMethodComparisonTests
{
    [Fact]
    public async Task Handler_WhenAiEnabled_BuildsComparisonContextAndRunsSkipperKeyedByGroup()
    {
        var runSettings = Substitute.For<IRunSettings>();
        runSettings.RunAiAnalysis.Returns(true);

        var context = new PerformanceNarrativeContext(Array.Empty<SailDiffCaseContext>(), "md", null);
        var builder = Substitute.For<IPerformanceNarrativeContextBuilder>();
        builder.BuildComparison(Arg.Any<MethodComparisonAnalysisCompleteNotification>()).Returns(context);
        var runner = Substitute.For<ISkipperAnalysisRunner>();

        var handler = new SkipperMethodComparisonAnalysisHandler(runSettings, builder, runner);
        var notification = new MethodComparisonAnalysisCompleteNotification("My Group", new[] { Pair(MethodComparisonVerdict.Slower) }, "md");

        await handler.Handle(notification, CancellationToken.None);

        builder.Received(1).BuildComparison(notification);
        await runner.Received(1).RunAsync(
            context,
            Arg.Is<string>(kind => kind.StartsWith("comparison")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handler_WhenAiDisabled_DoesNothing()
    {
        var runSettings = Substitute.For<IRunSettings>();
        runSettings.RunAiAnalysis.Returns(false);
        var builder = Substitute.For<IPerformanceNarrativeContextBuilder>();
        var runner = Substitute.For<ISkipperAnalysisRunner>();

        var handler = new SkipperMethodComparisonAnalysisHandler(runSettings, builder, runner);

        await handler.Handle(new MethodComparisonAnalysisCompleteNotification("G", new[] { Pair(MethodComparisonVerdict.Improved) }, "md"), CancellationToken.None);

        builder.DidNotReceiveWithAnyArgs().BuildComparison(default!);
        await runner.DidNotReceiveWithAnyArgs().RunAsync(default!, default!, default!);
    }

    [Fact]
    public async Task Handler_WhenNoPairs_DoesNothing()
    {
        var runSettings = Substitute.For<IRunSettings>();
        runSettings.RunAiAnalysis.Returns(true);
        var builder = Substitute.For<IPerformanceNarrativeContextBuilder>();
        var runner = Substitute.For<ISkipperAnalysisRunner>();

        var handler = new SkipperMethodComparisonAnalysisHandler(runSettings, builder, runner);

        await handler.Handle(new MethodComparisonAnalysisCompleteNotification("G", Array.Empty<MethodComparisonPairResult>(), "md"), CancellationToken.None);

        await runner.DidNotReceiveWithAnyArgs().RunAsync(default!, default!, default!);
    }

    [Fact]
    public void BuildComparison_MapsVerdictAndOrientsBaselineAsBefore()
    {
        var builder = new PerformanceNarrativeContextBuilder(
            Substitute.For<IReproducibilityManifestProvider>(),
            Substitute.For<IEnvironmentHealthReportProvider>());

        // Contender (compared) is faster than the baseline (primary): 5ms vs 10ms ⇒ Improved, -50%.
        var pair = new MethodComparisonPairResult(
            new MethodComparisonMember("Sut.Baseline", "Baseline", true, AResult()),
            new MethodComparisonMember("Sut.Candidate", "Candidate", false, AResult()),
            PrimaryMean: 10.0, ComparedMean: 5.0,
            PrimaryMedian: 10.0, ComparedMedian: 5.0,
            PrimarySampleSize: 8, ComparedSampleSize: 8,
            PValue: 0.001, QValue: 0.002,
            Ratio: 0.5, CiLower: 0.4, CiUpper: 0.6,
            Verdict: MethodComparisonVerdict.Improved);

        var context = builder.BuildComparison(
            new MethodComparisonAnalysisCompleteNotification("Group", new[] { pair }, "## md"));

        var c = context.Comparisons.ShouldHaveSingleItem();
        c.Verdict.ShouldBe(SkipperVerdict.Improved);
        c.MeanBefore.ShouldBe(10.0);  // baseline is "before"
        c.MeanAfter.ShouldBe(5.0);    // contender is "after"
        c.PercentChangeMean.ShouldBe(-50.0, 1e-9);
        c.AdjustedPValue.ShouldBe(0.002);
        c.EffectSizeValue.ShouldBe(0.5);
        context.SailDiffMarkdown.ShouldBe("## md");
    }

    private static MethodComparisonPairResult Pair(MethodComparisonVerdict verdict)
    {
        return new MethodComparisonPairResult(
            new MethodComparisonMember("Sut.A", "A", true, AResult()),
            new MethodComparisonMember("Sut.B", "B", false, AResult()),
            PrimaryMean: 10.0, ComparedMean: 12.0,
            PrimaryMedian: 10.0, ComparedMedian: 12.0,
            PrimarySampleSize: 5, ComparedSampleSize: 5,
            PValue: 0.04, QValue: 0.04,
            Ratio: 1.2, CiLower: 1.0, CiUpper: 1.4,
            Verdict: verdict);
    }

    private static PerformanceRunResult AResult()
    {
        return PerformanceRunResultBuilder.Create().Build();
    }
}
