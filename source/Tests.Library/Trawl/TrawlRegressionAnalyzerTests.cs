using System.Collections.Generic;
using NSubstitute;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Shouldly;
using Xunit;

namespace Tests.Library.Trawl;

public class TrawlRegressionAnalyzerTests
{
    private static readonly double[] Baseline = { 10, 11, 12, 10, 11 };
    private static readonly double[] Current = { 14, 15, 16, 15, 14 };
    private static SailDiffSettings Settings => new(alpha: 0.05, useOutlierDetection: false);

    private static IStatisticalTestExecutor StubReturning(double meanBefore, double meanAfter, double pValue)
    {
        var stat = new StatisticalTestResult(meanBefore, meanAfter, meanBefore, meanAfter, 0, pValue, "change", 5, 5,
            new double[0], new double[0], new Dictionary<string, object>());
        var exec = Substitute.For<IStatisticalTestExecutor>();
        exec.ExecuteStatisticalTest(Arg.Any<double[]>(), Arg.Any<double[]>(), Arg.Any<SailDiffSettings>())
            .Returns(new TestResultWithOutlierAnalysis(stat, null, null));
        return exec;
    }

    [Fact]
    public void SignificantSlowdown_IsRegressed_WithRequiredWording()
    {
        var verdict = new TrawlRegressionAnalyzer(StubReturning(10, 15, 0.001)).Compare(Baseline, Current, Settings);

        verdict.Outcome.ShouldBe(TrawlRegressionOutcome.Regressed);
        verdict.Message.ShouldContain("slower than baseline");
        verdict.PercentChange.ShouldBe(50, 0.001); // 10ms -> 15ms
    }

    [Fact]
    public void SignificantSpeedup_IsImproved()
    {
        var verdict = new TrawlRegressionAnalyzer(StubReturning(15, 10, 0.001)).Compare(Baseline, Current, Settings);

        verdict.Outcome.ShouldBe(TrawlRegressionOutcome.Improved);
        verdict.Message.ShouldContain("faster than baseline");
    }

    [Fact]
    public void HighPValue_IsNotSignificant()
    {
        var verdict = new TrawlRegressionAnalyzer(StubReturning(10, 15, 0.5)).Compare(Baseline, Current, Settings);

        verdict.Outcome.ShouldBe(TrawlRegressionOutcome.NotSignificant);
        verdict.Message.ShouldContain("NOT SIGNIFICANT");
    }

    [Fact]
    public void EmptyArrays_AreInconclusive()
    {
        new TrawlRegressionAnalyzer(StubReturning(10, 15, 0.001))
            .Compare(new double[0], Current, Settings).Outcome.ShouldBe(TrawlRegressionOutcome.Inconclusive);
    }
}
