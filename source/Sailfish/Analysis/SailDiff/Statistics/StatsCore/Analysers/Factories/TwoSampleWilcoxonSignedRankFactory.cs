using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Exceptions;
using System;
using System.Collections.Generic;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers.Factories;

internal static class TwoSampleWilcoxonSignedRankFactory
{
    public static TwoSampleWilcoxonSignedRank Create(
        IReadOnlyList<double> sample1,
        double[] sample2,
        TwoSampleHypothesis alternate = TwoSampleHypothesis.ValuesAreDifferent,
        bool? exact = null,
        bool adjustForTies = true)
    {
        if (sample1.Count != sample2.Length)
            throw new DimensionMismatchException(nameof(sample2), "Both samples should be of the same size.");
        var signs = new int[sample1.Count];
        var diffs = new double[sample1.Count];
        for (var index = 0; index < sample1.Count; ++index)
        {
            var num = sample1[index] - sample2[index];
            signs[index] = Math.Sign(num);
            diffs[index] = Math.Abs(num);
        }

        var test = new TwoSampleWilcoxonSignedRank(signs, diffs, alternate, exact, adjustForTies);

        return test;

    }
}