using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Analysis.Ai;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
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
            new PerformanceNarrativeContext(Array.Empty<SailDiffCaseContext>(), string.Empty),
            new CapabilityRegistry(Array.Empty<ISkipperCapability>()),
            "/tmp");

        var review = await agent.RunAsync(session, CancellationToken.None);

        review.HasContent.ShouldBeFalse();
    }
}

public class PerformanceNarrativeContextBuilderTests
{
    private const double Alpha = 0.05;
    private readonly PerformanceNarrativeContextBuilder builder = new();

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

    private PerformanceNarrativeContext Build(params SailDiffResult[] results) =>
        builder.Build(new SailDiffAnalysisCompleteNotification(results, "## markdown"), Alpha);

    private static SailDiffResult MakeResult(
        string name,
        double meanBefore = 0,
        double meanAfter = 0,
        double pValue = 1.0,
        double? qValue = null,
        bool failed = false)
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
                QValue = qValue
            };
        }

        return new SailDiffResult(new TestCaseId(name), new TestResultWithOutlierAnalysis(stats, null, null));
    }
}
