using System;
using System.Collections.Generic;
using MathNet.Numerics.Statistics;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers.Factories;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.MathOps;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Analysis.SailDiff.Statistics.Tests.TwoSampleWilcoxonSignedRankTestSailfish;

public interface ITwoSampleWilcoxonSignedRankTest : ITest;

/// <summary>
/// Two-sample Wilcoxon signed-rank test.
/// </summary>
/// <remarks>
/// <para>
/// <strong>This test is only valid for paired samples.</strong> Each element of <c>before</c>
/// must be paired with the element at the same index in <c>after</c> by experimental design
/// — e.g., the same input, the same iteration index in a deterministic experiment, or
/// repeated measures on the same subject. SailDiff's typical input (two independent benchmark
/// runs) does <strong>not</strong> satisfy this pairing assumption; for that scenario, prefer
/// <see cref="MWWilcoxonTestSailfish.MannWhitneyWilcoxonTest"/> (the default).
/// </para>
/// <para>
/// If <c>before</c> and <c>after</c> have different lengths the test cannot be computed and
/// the result will carry the underlying exception.
/// </para>
/// </remarks>
public class TwoSampleWilcoxonSignedRankTest : ITwoSampleWilcoxonSignedRankTest
{
    private readonly ITestPreprocessor _preprocessor;

    public TwoSampleWilcoxonSignedRankTest(ITestPreprocessor preprocessor)
    {
        _preprocessor = preprocessor;
    }

    public TestResultWithOutlierAnalysis ExecuteTest(double[] before, double[] after, SailDiffSettings settings)
    {
        var sigDig = settings.Round;

        try
        {
            // Signed-rank requires PAIRED samples. We pass int.MaxValue as max so no random
            // down-sample is performed; only outlier removal can shrink either side. Note that
            // independent per-sample outlier removal can break pairing — if the resulting sizes
            // differ, the factory throws DimensionMismatchException, which is caught below.
            // The recommended fix is to disable outlier detection on this test, or to switch to
            // MannWhitneyWilcoxonTest for independent benchmark samples.
            var (preprocessed1, preprocessed2) = _preprocessor.PreprocessJointlyWithDownSample(
                before, after, settings.UseOutlierDetection, minArraySize: 3, maxArraySize: int.MaxValue);
            var sample1 = preprocessed1.OutlierAnalysis?.DataWithOutliersRemoved ?? preprocessed1.RawData;
            var sample2 = preprocessed2.OutlierAnalysis?.DataWithOutliersRemoved ?? preprocessed2.RawData;

            var test = TwoSampleWilcoxonSignedRankFactory.Create(sample1, sample2);

            // Descriptive stats reported on the *processed* sample (post-outlier-removal /
            // alignment), matching every other test wrapper and the p-value just computed.
            // Pre-Tier-3 this branch reported `before.Mean()` / `after.Mean()` on the raw
            // input arrays, so the visible mean and the test verdict could describe
            // different data when outlier detection was on.
            var meanBefore = Math.Round(sample1.Mean(), sigDig);
            var meanAfter = Math.Round(sample2.Mean(), sigDig);
            var medianBefore = Math.Round(sample1.Median(), sigDig);
            var medianAfter = Math.Round(sample2.Median(), sigDig);
            var testStatistic = Math.Round(test.Statistic, sigDig);
            var pval = test.PValue;
            var isSignificant = pval <= settings.Alpha;
            var changeDirection = medianAfter > medianBefore ? SailfishChangeDirection.Regressed : SailfishChangeDirection.Improved;
            var description = isSignificant ? changeDirection : SailfishChangeDirection.NoChange;
            var additionalResults = new Dictionary<string, object>();
            var testResults = new StatisticalTestResult(
                meanBefore,
                meanAfter,
                medianBefore,
                medianAfter,
                testStatistic,
                pval,
                description,
                before.Length,
                after.Length,
                before,
                after,
                additionalResults);

            // MDE on the raw scale — consistent with the other wrappers. For paired data the
            // formula is a slight over-estimate (it ignores the pairing-induced correlation
            // reduction) but useful as an order-of-magnitude planning number.
            testResults.MinimumDetectableEffectPercent = MinimumDetectableEffect.RelativePercent(
                sample1.Mean(), sample1.Variance(), sample1.Length,
                sample2.Mean(), sample2.Variance(), sample2.Length,
                settings.Alpha);

            return new TestResultWithOutlierAnalysis(testResults, preprocessed1.OutlierAnalysis, preprocessed2.OutlierAnalysis);
        }
        catch (Exception ex)
        {
            return new TestResultWithOutlierAnalysis(ex);
        }
    }
}