using System;
using System.Collections.Generic;
using Accord.Statistics;
using Accord.Statistics.Testing;
using Sailfish.Analysis;
using Sailfish.Contracts;
using Sailfish.MathOps;

namespace Sailfish.Statistics.Tests.MWWilcoxonTest;

public class MannWhitneyWilcoxonTest : IMannWhitneyWilcoxonTest
{
    public class AdditionalResults
    {
        public const string Statistic1 = "Statistic1";
        public const string Statistic2 = "Statistic2";
    }

    private const int MinimumSampleSizeForTruncation = 10;

    public TestResults ExecuteTest(double[] before, double[] after, TestSettings settings)
    {
        var sigDig = settings.Round;

        Accord.Statistics.Testing.MannWhitneyWilcoxonTest test = null!;
        try
        { 
            test = new Accord.Statistics.Testing.MannWhitneyWilcoxonTest(
                PreProcessSample(before, settings),
                PreProcessSample(after, settings),
                TwoSampleHypothesis.ValuesAreDifferent);
        }
        catch (Exception ex)
        {
            ;
        }

        var meanBefore = Math.Round(before.Mean(), sigDig);
        var meanAfter = Math.Round(after.Mean(), sigDig);

        var medianBefore = Math.Round(before.Median(), sigDig);
        var medianAfter = Math.Round(after.Median(), sigDig);

        var testStatistic = Math.Round(test.Statistic, sigDig);
        var pVal = Math.Round(test.PValue, TestConstants.PValueSigDig);

        var isSignificant = test.PValue <= settings.Alpha;
        var changeDirection = meanAfter > meanBefore ? SailfishChangeDirection.Regressed : SailfishChangeDirection.Improved;

        var description = isSignificant ? changeDirection : SailfishChangeDirection.NoChange;

        var additionalResults = new Dictionary<string, double>
        {
            { AdditionalResults.Statistic1, test.Statistic1 },
            { AdditionalResults.Statistic2, test.Statistic2 }
        };

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