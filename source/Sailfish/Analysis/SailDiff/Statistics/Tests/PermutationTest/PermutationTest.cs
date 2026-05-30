using System;
using System.Collections.Generic;
using MathNet.Numerics.Statistics;
using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.MathOps;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Analysis.SailDiff.Statistics.Tests.PermutationTest;

public interface IPermutationTest : ITest;

/// <summary>
/// Two-sample permutation test on the difference in means.
/// </summary>
/// <remarks>
/// <para>
/// Distribution-free p-value: shuffles the joint pool of (sample1 ∪ sample2) labels
/// <c>K</c> times (default 10,000) and counts how often a random label assignment yields
/// a mean difference at least as extreme as the one actually observed. Reports the
/// two-tailed permutation p-value:
/// <c>p = (1 + #{|stat_k| ≥ |stat_obs|}) / (1 + K)</c>
/// — the +1 in numerator and denominator implements the recommended bias correction
/// (Phipson &amp; Smyth, 2010) so a sample of resamples never reports an impossible p = 0.
/// </para>
/// <para>
/// Effect-size and shift-estimate reports mirror the Mann-Whitney wrapper (Cliff's delta,
/// Hodges-Lehmann shift) since both are distribution-free and natural companions to a
/// permutation test on means.
/// </para>
/// <para>
/// Run order is deterministic when <see cref="IRunSettings.Seed"/> is set on the run
/// settings registered with the DI container; otherwise the underlying RNG seeds from
/// the system clock.
/// </para>
/// </remarks>
public class PermutationTest : IPermutationTest
{
    /// <summary>
    /// Default number of label-permutations used to estimate the null distribution. With
    /// 10,000 permutations the Monte Carlo standard error on a p ≈ 0.05 is ~0.002 — fine
    /// for typical α thresholds. Increase via <see cref="SailDiffSettings.PermutationCount"/>
    /// when you need higher resolution at very small p-values.
    /// </summary>
    public const int DefaultPermutationCount = 10_000;

    private readonly ITestPreprocessor _preprocessor;
    private readonly IRunSettings? _runSettings;

    public PermutationTest(ITestPreprocessor preprocessor)
    {
        _preprocessor = preprocessor;
        _runSettings = null;
    }

    /// <summary>
    /// Preferred ctor — consumes <see cref="IRunSettings.Seed"/> via DI so the resample
    /// stream is reproducible across runs.
    /// </summary>
    public PermutationTest(ITestPreprocessor preprocessor, IRunSettings runSettings)
        : this(preprocessor)
    {
        _runSettings = runSettings;
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

            if (sample1.Length < 2 || sample2.Length < 2)
                throw new ArgumentException("Permutation test requires at least 2 observations per side.");

            var observed = sample2.Mean() - sample1.Mean();
            var observedAbs = Math.Abs(observed);

            var k = settings.PermutationCount > 0 ? settings.PermutationCount : DefaultPermutationCount;
            var rng = new Random(_runSettings?.Seed ?? Environment.TickCount);

            // Pool the two samples and resample labels K times. We don't permute in place
            // because we need the originals to stay correct for the descriptive stats below.
            var pooled = new double[sample1.Length + sample2.Length];
            Array.Copy(sample1, 0, pooled, 0, sample1.Length);
            Array.Copy(sample2, 0, pooled, sample1.Length, sample2.Length);

            // Fisher-Yates shuffle is overkill for this — we can compute mean-of-first-N and
            // mean-of-rest in O(N) per permutation via partial-sum tracking. Simpler approach:
            // shuffle the pooled array each iteration and split. O(K · N) total. For K=10K
            // and N≤1000 that's 10M ops — well under a second.
            var countAtLeastAsExtreme = 0;
            var shuffled = (double[])pooled.Clone();
            for (var iter = 0; iter < k; iter++)
            {
                ShuffleInPlace(shuffled, rng);

                // Mean of first n1 elements vs mean of the remaining n2.
                var sum1 = 0.0;
                for (var i = 0; i < sample1.Length; i++) sum1 += shuffled[i];
                var mean1 = sum1 / sample1.Length;
                var sum2 = 0.0;
                for (var i = sample1.Length; i < shuffled.Length; i++) sum2 += shuffled[i];
                var mean2 = sum2 / sample2.Length;

                var permDiff = mean2 - mean1;
                if (Math.Abs(permDiff) >= observedAbs) countAtLeastAsExtreme++;
            }

            // Phipson & Smyth (2010): (1 + count) / (1 + K) avoids the impossible p = 0
            // from a finite Monte Carlo sample.
            var pVal = (1.0 + countAtLeastAsExtreme) / (1.0 + k);
            var isSignificant = pVal <= settings.Alpha;

            var meanBefore = Math.Round(sample1.Mean(), sigDig);
            var meanAfter = Math.Round(sample2.Mean(), sigDig);
            var medianBefore = Math.Round(sample1.Median(), sigDig);
            var medianAfter = Math.Round(sample2.Median(), sigDig);
            var testStatistic = Math.Round(observed, sigDig);
            var changeDirection = meanAfter > meanBefore
                ? SailfishChangeDirection.Regressed
                : SailfishChangeDirection.Improved;
            var description = isSignificant ? changeDirection : SailfishChangeDirection.NoChange;

            var additionalResults = new Dictionary<string, object>
            {
                { AdditionalResults.PermutationCount, k },
                { AdditionalResults.CountAtLeastAsExtreme, countAtLeastAsExtreme }
            };

            // SampleSize* / RawData* describe the data the test actually consumed (after
            // outlier removal), matching the mean/median/p-value already computed from the
            // processed sample. The original user input is still accessible on the wrapping
            // TestResultWithOutlierAnalysis via Sample1.OriginalData / Sample2.OriginalData.
            var testResults = new StatisticalTestResult(
                meanBefore,
                meanAfter,
                medianBefore,
                medianAfter,
                testStatistic,
                Math.Round(pVal, TestConstants.PValueSigDig),
                description,
                sample1.Length,
                sample2.Length,
                sample1,
                sample2,
                additionalResults);

            // Distribution-free effect size + shift — same suite the Mann-Whitney wrapper
            // reports, for visual consistency in the output.
            testResults.EffectSize = EffectSizes.CliffsDelta(sample1, sample2, settings.Alpha);
            testResults.Difference = EffectSizes.HodgesLehmann(sample1, sample2, settings.Alpha);
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

    private static void ShuffleInPlace(double[] arr, Random rng)
    {
        for (var i = arr.Length - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
    }

    public record AdditionalResults
    {
        public const string PermutationCount = "PermutationCount";
        public const string CountAtLeastAsExtreme = "CountAtLeastAsExtreme";
    }
}
