using System.Linq;
using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Sailfish.Analysis.SailDiff.Statistics.Tests.MWWilcoxonTestSailfish;
using Sailfish.Analysis.SailDiff.Statistics.Tests.TwoSampleWilcoxonSignedRankTestSailfish;
using Sailfish.Contracts.Public;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.SailDiff;

/// <summary>
/// Regression coverage for the change-direction verdict when display rounding (settings.Round,
/// default 3 → ms with 3 decimals) collapses two genuinely-different sub-millisecond statistics
/// to the same displayed value. The verdict must follow the <em>unrounded</em> statistic; deciding
/// it from the rounded value sent every such case to the "Improved" branch — because
/// <c>after &gt; before</c> is false when both round equal — mislabelling regressions as improvements.
/// </summary>
public class ChangeDirectionRoundingTests
{
    private static TestPreprocessor Preprocessor() => new(new SailfishOutlierDetector());

    private static SailDiffSettings Settings(TestType testType) =>
        new(alpha: 0.05, round: 3, useOutlierDetection: false, testType: testType);

    // 10 distinct, strictly increasing sub-millisecond values; every value rounds to 0.001 ms
    // at Round = 3, so the displayed mean/median collapses to a single value.
    private static double[] Cluster(double centerMs) =>
        Enumerable.Range(0, 10).Select(i => centerMs + i * 1e-6).ToArray();

    [Fact]
    public void RankSum_AfterSlightlySlower_RoundsEqual_StillReportsRegressed()
    {
        var before = Cluster(0.000590); // ≈ 0.59 µs
        var after = Cluster(0.000620); // ≈ 0.62 µs, every value > every before value

        var result = new MannWhitneyWilcoxonTest(Preprocessor())
            .ExecuteTest(before, after, Settings(TestType.WilcoxonRankSumTest));

        result.ExceptionMessage.ShouldBeEmpty();
        result.StatisticalTestResult.PValue.ShouldBeLessThan(0.05); // significant
        // Precondition: rounding has collapsed both means to the same displayed value.
        result.StatisticalTestResult.MeanBefore.ShouldBe(result.StatisticalTestResult.MeanAfter);
        // After is genuinely slower → must be Regressed, not Improved.
        result.StatisticalTestResult.ChangeDescription.ShouldBe(SailfishChangeDirection.Regressed);
    }

    [Fact]
    public void RankSum_AfterSlightlyFaster_RoundsEqual_StillReportsImproved()
    {
        var before = Cluster(0.000620);
        var after = Cluster(0.000590); // every value < every before value

        var result = new MannWhitneyWilcoxonTest(Preprocessor())
            .ExecuteTest(before, after, Settings(TestType.WilcoxonRankSumTest));

        result.ExceptionMessage.ShouldBeEmpty();
        result.StatisticalTestResult.PValue.ShouldBeLessThan(0.05);
        result.StatisticalTestResult.MeanBefore.ShouldBe(result.StatisticalTestResult.MeanAfter);
        result.StatisticalTestResult.ChangeDescription.ShouldBe(SailfishChangeDirection.Improved);
    }

    [Fact]
    public void SignedRank_MedianDirection_RoundsEqual_StillReportsRegressed()
    {
        // The signed-rank wrapper decides direction from the median rather than the mean; same
        // collapse, same requirement that the unrounded value drives the verdict.
        var before = Cluster(0.000590);
        var after = Cluster(0.000620);

        var result = new TwoSampleWilcoxonSignedRankTest(Preprocessor())
            .ExecuteTest(before, after, Settings(TestType.TwoSampleWilcoxonSignedRankTest));

        result.ExceptionMessage.ShouldBeEmpty();
        result.StatisticalTestResult.PValue.ShouldBeLessThan(0.05);
        result.StatisticalTestResult.MedianBefore.ShouldBe(result.StatisticalTestResult.MedianAfter);
        result.StatisticalTestResult.ChangeDescription.ShouldBe(SailfishChangeDirection.Regressed);
    }
}
