using System;
using System.Collections.Generic;
using MathNet.Numerics.Statistics;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers.Factories;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Analysis.SailDiff.Statistics.Tests.MWWilcoxonTestSailfish;

public interface IMannWhitneyWilcoxonTest : ITest;

/// <summary>
/// Wilcoxon Rank-Sum / Mann-Whitney U test for two independent samples.
/// </summary>
/// <remarks>
/// <para>
/// This is the recommended SailDiff test for comparing two independent benchmark runs.
/// The underlying <see cref="StatsCore.Distributions.MannWhitneyDistribution"/> uses the
/// exact null distribution when both samples have N ≤ 30, and switches to the large-sample
/// normal approximation (with continuity correction and tie correction) otherwise — see
/// <c>MannWhitneyDistribution.Exact</c>. Either way, this wrapper runs the test
/// <em>once</em> on the full preprocessed sample; it does <strong>not</strong> downsample,
/// resample, or average p-values across resamples.
/// </para>
/// <para>
/// Reported means and medians are computed on the same preprocessed sample the test
/// consumed — outliers removed when <see cref="SailDiffSettings.UseOutlierDetection"/> is
/// true — so descriptive statistics and the p-value describe the same data.
/// </para>
/// </remarks>
public class MannWhitneyWilcoxonTest : IMannWhitneyWilcoxonTest
{
    private readonly ITestPreprocessor _preprocessor;

    public MannWhitneyWilcoxonTest(ITestPreprocessor preprocessor)
    {
        _preprocessor = preprocessor;
    }

    public TestResultWithOutlierAnalysis ExecuteTest(double[] before, double[] after, SailDiffSettings settings)
    {
        var sigDig = settings.Round;

        try
        {
            var preprocessed1 = _preprocessor.Preprocess(before, settings.UseOutlierDetection);
            var preprocessed2 = _preprocessor.Preprocess(after, settings.UseOutlierDetection);

            var sample1 = preprocessed1.OutlierAnalysis?.DataWithOutliersRemoved ?? preprocessed1.RawData;
            var sample2 = preprocessed2.OutlierAnalysis?.DataWithOutliersRemoved ?? preprocessed2.RawData;

            var test = MannWhitneyWilcoxonFactory.Create(sample1, sample2);

            var meanBefore = Math.Round(sample1.Mean(), sigDig);
            var meanAfter = Math.Round(sample2.Mean(), sigDig);
            var medianBefore = Math.Round(sample1.Median(), sigDig);
            var medianAfter = Math.Round(sample2.Median(), sigDig);
            var testStatistic = Math.Round(test.Statistic, sigDig);
            var pVal = Math.Round(test.PValue, TestConstants.PValueSigDig);
            var isSignificant = test.PValue <= settings.Alpha;
            var description = isSignificant
                ? meanAfter > meanBefore ? SailfishChangeDirection.Regressed : SailfishChangeDirection.Improved
                : SailfishChangeDirection.NoChange;
            var additionalResults = new Dictionary<string, object>
            {
                { AdditionalResults.Statistic1, test.Statistic1 },
                { AdditionalResults.Statistic2, test.Statistic2 }
            };

            var testResults = new StatisticalTestResult(
                meanBefore,
                meanAfter,
                medianBefore,
                medianAfter,
                testStatistic,
                pVal,
                description,
                before.Length,
                after.Length,
                before,
                after,
                additionalResults);

            // Effect size: Cliff's delta (P(after > before) − P(after < before)). Natural
            // companion to the rank-sum test — distribution-free, bounded in [−1, 1], directly
            // interpretable as stochastic dominance probability.
            testResults.EffectSize = EffectSizes.CliffsDelta(sample1, sample2, settings.Alpha);

            // Difference: Hodges-Lehmann shift estimator — median of all pairwise differences
            // (after − before). Robust, distribution-free, and paired with a CI built from
            // the rank-sum critical value at the user's α.
            testResults.Difference = EffectSizes.HodgesLehmann(sample1, sample2, settings.Alpha);

            return new TestResultWithOutlierAnalysis(testResults, preprocessed1.OutlierAnalysis, preprocessed2.OutlierAnalysis);
        }
        catch (Exception ex)
        {
            return new TestResultWithOutlierAnalysis(ex);
        }
    }

    public record AdditionalResults
    {
        public const string Statistic1 = "Statistic1";
        public const string Statistic2 = "Statistic2";
    }
}
