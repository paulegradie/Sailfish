using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers.AnalysersBase;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionBase;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Exceptions;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.MathOps;
using System;
using System.Collections.Generic;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers.Factories;

internal static class TwoSampleWilcoxonSignedRankFactory
{
    public static TwoSampleWilcoxonSignedRank Create(
        IReadOnlyList<double> sample1,
        double[] sample2,
        DistributionTailSailfish tailType = DistributionTailSailfish.TwoTail)
    {
        if (sample1.Count != sample2.Length)
        {
            throw new DimensionMismatchException(nameof(sample2), "Both samples should be of the same size.");
        }

        var signs = new int[sample1.Count];
        var diffs = new double[sample1.Count];
        for (var index = 0; index < sample1.Count; ++index)
        {
            var difference = sample1[index] - sample2[index];
            signs[index] = Math.Sign(difference);
            diffs[index] = Math.Abs(difference);
        }

        var indexes = diffs.Find(x => x != 0.0);
        var hasZeros = indexes.Length != diffs.Length;
        if (hasZeros)
        {
            signs = signs.Get(indexes);
            diffs = diffs.Get(indexes);
        }

        var ranks = diffs.Rank();
        if (ranks.Length == 0)
        {
            throw new ArgumentException($"The {nameof(TwoSampleWilcoxonSignedRank)} must be provided valid samples");
        }

        return new TwoSampleWilcoxonSignedRank(signs, ranks, tailType);
    }
}