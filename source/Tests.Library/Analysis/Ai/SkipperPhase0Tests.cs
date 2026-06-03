using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Analysis.Ai;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Diagnostics.Environment;
using Sailfish.Results;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.Ai;

public class SkipperReviewTests
{
    [Fact]
    public void Empty_HasNoContent()
    {
        SkipperReview.Empty.HasContent.ShouldBeFalse();
        SkipperReview.Empty.OverallVerdict.ShouldBe(SkipperVerdict.Inconclusive);
    }

    [Fact]
    public void HasContent_TrueWhenConsoleSummaryPresent()
    {
        var review = SkipperReview.Empty with { ConsoleSummary = "Skipper says hi" };
        review.HasContent.ShouldBeTrue();
    }

    [Fact]
    public void HasContent_TrueWhenFindingsPresent()
    {
        var review = new SkipperReview(
            SkipperVerdict.Regressed,
            new[] { new Finding("Bench.Method", SkipperVerdict.Regressed, "slower", Array.Empty<string>(), 0.9) },
            Array.Empty<ProposedAction>(),
            string.Empty,
            string.Empty);

        review.HasContent.ShouldBeTrue();
    }
}

public class CapabilityRegistryTests
{
    [Fact]
    public void GrantedCapability_IsDiscoverable()
    {
        const string repoRoot = "/tmp/repo";
        var registry = new CapabilityRegistry(new ISkipperCapability[] { new CodeReadCapability(repoRoot) });

        registry.Has<ICodeReadCapability>().ShouldBeTrue();
        registry.Get<ICodeReadCapability>().ShouldNotBeNull();
        registry.Get<ICodeReadCapability>()!.RepositoryRoot.ShouldBe(repoRoot);
        registry.Granted.Count.ShouldBe(1);
    }

    [Fact]
    public void UngrantedCapability_IsAbsent()
    {
        var registry = new CapabilityRegistry(new ISkipperCapability[] { new CodeReadCapability("/tmp/repo") });

        registry.Has<ITelemetryQueryCapability>().ShouldBeFalse();
        registry.Get<ITelemetryQueryCapability>().ShouldBeNull();
    }
}

public class NoOpSailfishAgentTests
{
    [Fact]
    public async Task ReturnsEmptyReview()
    {
        var agent = new NoOpSailfishAgent();
        var session = new SkipperSession(
            SkipperRole.Explain,
            new PerformanceNarrativeContext(Array.Empty<SailDiffCaseContext>(), string.Empty, null),
            new CapabilityRegistry(Array.Empty<ISkipperCapability>()),
            "/tmp");

        var review = await agent.RunAsync(session, CancellationToken.None);

        review.HasContent.ShouldBeFalse();
    }
}

public class PerformanceNarrativeContextBuilderTests
{
    private const double Alpha = 0.05;
    private readonly PerformanceNarrativeContextBuilder builder = new(
        Substitute.For<IReproducibilityManifestProvider>(),
        Substitute.For<IEnvironmentHealthReportProvider>());

    [Fact]
    public void SignificantSlowdown_IsRegressed_WithCorrectPercentChange()
    {
        var context = Build(MakeResult("A", meanBefore: 100, meanAfter: 118, pValue: 0.001));

        var c = context.Comparisons.Single();
        c.Verdict.ShouldBe(SkipperVerdict.Regressed);
        c.PercentChangeMean.ShouldBe(18.0, 1e-9);
    }

    [Fact]
    public void SignificantSpeedup_IsImproved()
    {
        var context = Build(MakeResult("A", meanBefore: 100, meanAfter: 80, pValue: 0.001));
        context.Comparisons.Single().Verdict.ShouldBe(SkipperVerdict.Improved);
    }

    [Fact]
    public void NonSignificant_IsNotSignificant_RegardlessOfDirection()
    {
        var context = Build(MakeResult("A", meanBefore: 100, meanAfter: 130, pValue: 0.42));
        context.Comparisons.Single().Verdict.ShouldBe(SkipperVerdict.NotSignificant);
    }

    [Fact]
    public void AdjustedQValue_IsPreferredOverRawPValue()
    {
        // Raw p is significant, but the BH-FDR q-value is not → the family-wise verdict wins.
        var context = Build(MakeResult("A", meanBefore: 100, meanAfter: 130, pValue: 0.001, qValue: 0.20));
        context.Comparisons.Single().Verdict.ShouldBe(SkipperVerdict.NotSignificant);
    }

    [Fact]
    public void FailedResult_IsInconclusive()
    {
        var context = Build(MakeResult("A", failed: true));
        var c = context.Comparisons.Single();
        c.Verdict.ShouldBe(SkipperVerdict.Inconclusive);
        c.Failed.ShouldBeTrue();
    }

    [Fact]
    public void EffectSizeAndMde_FlowIntoContext()
    {
        var context = Build(MakeResult("A", meanBefore: 100, meanAfter: 118, pValue: 0.001,
            effectSize: new EffectSizeReport("Cliff's delta", 0.8, 0.5, 0.95),
            minimumDetectableEffectPercent: 3.2));

        var c = context.Comparisons.Single();
        c.EffectSizeName.ShouldBe("Cliff's delta");
        c.EffectSizeValue!.Value.ShouldBe(0.8, 1e-9);
        c.MinimumDetectableEffectPercent!.Value.ShouldBe(3.2, 1e-9);
    }

    [Fact]
    public void SubMillisecondChange_UsesFullPrecisionMeans_NotRoundedScalars()
    {
        // Real data: before ≈ 0.0006 ms, after ≈ 0.000625 ms (a ~4% regression). The statistical
        // tests pre-round the scalar means to SailDiffSettings.Round (3) → both 0.001, which would
        // zero the percent-change and flip the verdict to Improved. The context must recompute from
        // the raw arrays so the agent sees the real, correctly-directed change.
        var before = Enumerable.Range(0, 10).Select(i => 0.000595 + i * 1e-6).ToArray();
        var after = Enumerable.Range(0, 10).Select(i => 0.000620 + i * 1e-6).ToArray();
        var stats = new StatisticalTestResult(
            meanBefore: Math.Round(before.Average(), 3),  // 0.001 — the pre-rounded scalar
            meanAfter: Math.Round(after.Average(), 3),    // 0.001 — identical once rounded
            medianBefore: Math.Round(before.Average(), 3),
            medianAfter: Math.Round(after.Average(), 3),
            testStatistic: 0, pValue: 0.01, changeDescription: "desc",
            sampleSizeBefore: 10, sampleSizeAfter: 10,
            rawDataBefore: before, rawDataAfter: after,
            additionalResults: new Dictionary<string, object>());
        var result = new SailDiffResult(new TestCaseId("WithJoin"), new TestResultWithOutlierAnalysis(stats, null, null));

        var c = builder.Build(new SailDiffAnalysisCompleteNotification(new[] { result }, "## md"), Alpha).Comparisons.Single();

        c.MeanBefore.ShouldBe(before.Average(), 1e-12); // full precision, not the rounded 0.001
        c.MeanAfter.ShouldBe(after.Average(), 1e-12);
        c.MeanBefore.ShouldNotBe(c.MeanAfter);          // would have been equal with the rounded scalars
        c.PercentChangeMean.ShouldBeGreaterThan(3.0);   // the real change is visible, not zeroed
        c.Verdict.ShouldBe(SkipperVerdict.Regressed);   // and correctly directed
    }

    private PerformanceNarrativeContext Build(params SailDiffResult[] results) =>
        builder.Build(new SailDiffAnalysisCompleteNotification(results, "## markdown"), Alpha);

    private static SailDiffResult MakeResult(
        string name,
        double meanBefore = 0,
        double meanAfter = 0,
        double pValue = 1.0,
        double? qValue = null,
        bool failed = false,
        EffectSizeReport? effectSize = null,
        double? minimumDetectableEffectPercent = null)
    {
        StatisticalTestResult stats;
        if (failed)
        {
            stats = new StatisticalTestResult(new Exception("boom"));
        }
        else
        {
            stats = new StatisticalTestResult(
                meanBefore, meanAfter,
                medianBefore: meanBefore, medianAfter: meanAfter,
                testStatistic: 0,
                pValue: pValue,
                changeDescription: "desc",
                sampleSizeBefore: 10, sampleSizeAfter: 10,
                rawDataBefore: Array.Empty<double>(), rawDataAfter: Array.Empty<double>(),
                additionalResults: new Dictionary<string, object>())
            {
                QValue = qValue,
                EffectSize = effectSize,
                MinimumDetectableEffectPercent = minimumDetectableEffectPercent
            };
        }

        return new SailDiffResult(new TestCaseId(name), new TestResultWithOutlierAnalysis(stats, null, null));
    }
}
