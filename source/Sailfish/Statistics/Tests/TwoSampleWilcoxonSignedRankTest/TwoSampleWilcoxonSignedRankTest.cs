using System;
using System.Collections.Generic;
using Accord.Statistics;
using Accord.Statistics.Testing;
using Sailfish.Analysis;
using Sailfish.Contracts;
using Sailfish.MathOps;

namespace Sailfish.Statistics.Tests.TwoSampleWilcoxonSignedRankTest;

public class TwoSampleWilcoxonSignedRankTest : ITwoSampleWilcoxonSignedRankTest
{
    private const int MinimumSampleSizeForTruncation = 10;

    public TestResults ExecuteTest(double[] before, double[] after, TestSettings settings)
    {
        var sigDig = settings.Round;


        var test = new Accord.Statistics.Testing.TwoSampleWilcoxonSignedRankTest(
            PreProcessSample(before, settings),
            PreProcessSample(after, settings),
            TwoSampleHypothesis.ValuesAreDifferent);

        var meanBefore = Math.Round(before.Mean(), sigDig);
        var meanAfter = Math.Round(after.Mean(), sigDig);

        var medianBefore = Math.Round(before.Median(), sigDig);
        var medianAfter = Math.Round(after.Median(), sigDig);

        var testStatistic = Math.Round(test.Statistic, sigDig);
        var pVal = Math.Round(test.PValue, TestConstants.PValueSigDig);

        var isSignificant = test.PValue <= settings.Alpha;
        var changeDirection = medianAfter > medianBefore ? SailfishChangeDirection.Regressed : SailfishChangeDirection.Improved;

        var description = isSignificant ? changeDirection : SailfishChangeDirection.NoChange;

        var additionalResults = new Dictionary<string, double>();

        return new TestResults(
            meanBefore,
            meanAfter,
            medianBefore,
            medianAfter,
            testStatistic,
            pVal,
            description,
            before.Length,
            after.Length,
            additionalResults);
    }

    private static double[] PreProcessSample(double[] rawData, TestSettings settings)
    {
        if (!settings.UseInnerQuartile) return rawData;
        if (rawData.Length < MinimumSampleSizeForTruncation) return rawData;
        var quartiles = ComputeQuartiles.GetInnerQuartileValues(rawData);
        return quartiles;
    }
}