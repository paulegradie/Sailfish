using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers.AnalysersBase;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionBase;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers;

/// <summary>
/// Two-sample Kolmogorov-Smirnov test statistic and asymptotic p-value.
/// </summary>
/// <remarks>
/// <para>
/// The statistic is the supremum of <c>|F1(x) − F2(x)|</c> (two-sided), <c>sup(F2 − F1)</c>
/// (alternative: sample 1 stochastically larger than sample 2 ⇒ F1 ≤ F2), or
/// <c>sup(F1 − F2)</c> (alternative: sample 1 stochastically smaller). Because both empirical
/// CDFs are step functions that jump only at observed values, the supremum is attained at
/// some point in <c>sample1 ∪ sample2</c> — we walk the sorted union and evaluate the CDFs
/// directly.
/// </para>
/// <para>
/// Pre-Tier-2 the implementation evaluated <c>F1</c> at <c>array[i]</c> and <c>F2</c> at
/// <c>array[i + 1]</c> — an off-by-one that produced a non-zero D value (= 1/N) even for
/// identical samples, where the correct answer is exactly 0. See
/// <c>KolmogorovSmirnovGoldenTests.Identical_Samples_ProduceZeroDStatistic</c>.
/// </para>
/// </remarks>
internal sealed class TwoSampleKolmogorovSmirnov : HypothesisTest
{
    public TwoSampleKolmogorovSmirnov(
        double[] sample1,
        double[] sample2,
        TwoSampleKolmogorovSmirnovTestHypothesis alternate = TwoSampleKolmogorovSmirnovTestHypothesis.SamplesDistributionsAreUnequal)
    {
        Hypothesis = alternate;
        var length1 = sample1.Length;
        var length2 = sample2.Length;
        StatisticDistribution = new KolmogorovSmirnovDistribution(length1 * length2 / (double)(length1 + length2));
        EmpiricalDistribution1 = new EmpiricalDistribution(sample1, 0.0);
        EmpiricalDistribution2 = new EmpiricalDistribution(sample2, 0.0);

        // Walk the union of both samples sorted ascending. The supremum is attained at one
        // of these points because both empirical CDFs are constant between observations.
        var union = new double[length1 + length2];
        Array.Copy(sample1, 0, union, 0, length1);
        Array.Copy(sample2, 0, union, length1, length2);
        Array.Sort(union);

        var func1 = EmpiricalDistribution1.DistributionFunction;
        var func2 = EmpiricalDistribution2.DistributionFunction;

        var statistic = 0.0;
        switch (alternate)
        {
            case TwoSampleKolmogorovSmirnovTestHypothesis.SamplesDistributionsAreUnequal:
                foreach (var x in union)
                {
                    var diff = Math.Abs(func1(x) - func2(x));
                    if (diff > statistic) statistic = diff;
                }
                Statistic = statistic;
                PValue = StatisticDistribution.ComplementaryDistributionFunction(Statistic);
                break;

            case TwoSampleKolmogorovSmirnovTestHypothesis.FirstSampleIsLargerThanSecond:
                // X1 stochastically larger ⇒ for every x, P(X1 ≤ x) ≤ P(X2 ≤ x), i.e. F1 ≤ F2.
                // Test statistic D+ = sup(F2 − F1) is large when the alternative holds.
                foreach (var x in union)
                {
                    var diff = func2(x) - func1(x);
                    if (diff > statistic) statistic = diff;
                }
                Statistic = statistic;
                PValue = StatisticDistribution.OneSideDistributionFunction(Statistic);
                break;

            default: // FirstSampleIsSmallerThanSecond
                foreach (var x in union)
                {
                    var diff = func1(x) - func2(x);
                    if (diff > statistic) statistic = diff;
                }
                Statistic = statistic;
                PValue = StatisticDistribution.OneSideDistributionFunction(Statistic);
                break;
        }

        Tail = (DistributionTailSailfish)alternate;
    }

    public TwoSampleKolmogorovSmirnovTestHypothesis Hypothesis { get; private set; }

    public EmpiricalDistribution EmpiricalDistribution1 { get; }

    public EmpiricalDistribution EmpiricalDistribution2 { get; }
    public KolmogorovSmirnovDistribution StatisticDistribution { get; set; }
}
