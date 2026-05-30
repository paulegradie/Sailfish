using System;
using System.Collections.Generic;
using MathNet.Numerics.Statistics;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers.Factories;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.MathOps;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Analysis.SailDiff.Statistics.Tests.TTest;

public interface ITTest : ITest;

public class Test : ITTest
{
    private readonly ITestPreprocessor _preprocessor;

    public Test(ITestPreprocessor preprocessor)
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
            var rawSample1 = preprocessed1.OutlierAnalysis?.DataWithOutliersRemoved ?? preprocessed1.RawData;
            var rawSample2 = preprocessed2.OutlierAnalysis?.DataWithOutliersRemoved ?? preprocessed2.RawData;

            // Choose the sample the test will operate on. When LogTransform is enabled the
            // t-test runs on log(time); the resulting CI in log-space exponentiates to a
            // multiplicative ratio rather than an additive ms delta. Both samples must be
            // strictly positive — non-positive entries trigger a graceful fall-through to
            // raw-scale testing so the wrapper never throws on edge data.
            var useLogTransform = settings.LogTransform && AllPositive(rawSample1) && AllPositive(rawSample2);
            var sample1 = useLogTransform ? LogTransform(rawSample1) : rawSample1;
            var sample2 = useLogTransform ? LogTransform(rawSample2) : rawSample2;

            // Welch's t-test: independent samples, no equal-variance assumption. Passing the
            // user's alpha makes the test's reported CI pair with the same threshold used for
            // the significance decision — 95% CI for alpha=0.05, 99% for alpha=0.01, etc.
            var test = TwoSampleTFactory.Create(sample1, sample2, false, alpha: settings.Alpha);

            // Means / medians reported on the raw scale so the user sees their familiar ms
            // numbers regardless of whether the test ran on log(time).
            var meanBefore = Math.Round(rawSample1.Mean(), sigDig);
            var meanAfter = Math.Round(rawSample2.Mean(), sigDig);
            var medianBefore = Math.Round(rawSample1.Median(), sigDig);
            var medianAfter = Math.Round(rawSample2.Median(), sigDig);
            var testStatistic = Math.Round(test.Statistic, sigDig);
            var dof = Math.Round(test.DegreesOfFreedom, sigDig);
            var isSignificant = test.PValue <= settings.Alpha;
            var pVal = Math.Round(test.PValue, TestConstants.PValueSigDig);
            var changeDirection = meanAfter > meanBefore ? SailfishChangeDirection.Regressed : SailfishChangeDirection.Improved;
            var description = isSignificant ? changeDirection : SailfishChangeDirection.NoChange;
            var additionalResults = new Dictionary<string, object> { { AdditionalResults.DegreesOfFreedom, dof } };
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
                pVal,
                description,
                rawSample1.Length,
                rawSample2.Length,
                rawSample1,
                rawSample2,
                additionalResults);

            // Effect size: Hedges' g on the *raw* scale so the reported magnitude is
            // interpretable in standard "small / medium / large" terms regardless of whether
            // the test itself ran on log-time.
            testResults.EffectSize = EffectSizes.HedgesG(
                rawSample1.Mean(), rawSample1.Variance(), rawSample1.Length,
                rawSample2.Mean(), rawSample2.Variance(), rawSample2.Length,
                settings.Alpha);

            // Difference: derived from whatever sample the test ran on. Log-transform mode
            // exponentiates the bounds back to a multiplicative ratio.
            if (useLogTransform)
            {
                // Log-ratio shift: exp(diff) is the ratio "after / before". 1.10 means 10%
                // slower. CI bounds exponentiate the same way.
                var ratio = Math.Exp(test.EstimatedValue2 - test.EstimatedValue1);
                double? ciLower = test.Confidence is { Min: var min } ? Math.Exp(-min) : (double?)null;
                double? ciUpper = test.Confidence is { Max: var max } ? Math.Exp(-max) : (double?)null;
                // The TwoSampleT CI was built on (mean1 − mean2); flip+exp gives (after/before).
                // After the sign-flip-exp, the original "lower of (1-2)" becomes the upper of
                // (2/1), so re-sort to make ciLower < ciUpper.
                if (ciLower.HasValue && ciUpper.HasValue && ciLower.Value > ciUpper.Value)
                    (ciLower, ciUpper) = (ciUpper, ciLower);
                testResults.Difference = new DifferenceReport(
                    "Log-ratio (after / before)", ratio, ciLower, ciUpper, "× ratio");
            }
            else
            {
                // Raw-scale mean difference with Welch CI at the user's α. The analyser builds
                // its CI on (mean1 − mean2); EffectSizes.MeanDifference flips the sign so the
                // report follows the After − Before convention used everywhere else in SailDiff.
                testResults.Difference = EffectSizes.MeanDifference(
                    test.EstimatedValue1, test.EstimatedValue2,
                    test.Confidence.Min, test.Confidence.Max);
            }

            // MDE on the raw scale, expressed as a percentage of the pooled mean. Answers
            // "what's the smallest regression this run could have caught?" — actionable when
            // the result was NoChange but you suspect a real but small effect was missed.
            testResults.MinimumDetectableEffectPercent = MinimumDetectableEffect.RelativePercent(
                rawSample1.Mean(), rawSample1.Variance(), rawSample1.Length,
                rawSample2.Mean(), rawSample2.Variance(), rawSample2.Length,
                settings.Alpha);

            return new TestResultWithOutlierAnalysis(testResults, preprocessed1.OutlierAnalysis, preprocessed2.OutlierAnalysis);
        }
        catch (Exception ex)
        {
            return new TestResultWithOutlierAnalysis(ex);
        }
    }

    public record AdditionalResults
    {
        public const string DegreesOfFreedom = "DegreesOfFreedom";
    }

    private static bool AllPositive(double[] sample)
    {
        for (var i = 0; i < sample.Length; i++)
            if (!(sample[i] > 0))
                return false;
        return true;
    }

    private static double[] LogTransform(double[] sample)
    {
        var transformed = new double[sample.Length];
        for (var i = 0; i < sample.Length; i++)
            transformed[i] = Math.Log(sample[i]);
        return transformed;
    }
}