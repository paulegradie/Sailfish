using System;
using System.Collections.Generic;
using Accord.Statistics;
using Accord.Statistics.Testing;
using Sailfish.Analysis;
using Sailfish.Contracts;
using Sailfish.MathOps;

namespace Sailfish.Statistics.Tests.TTest;

internal class TTest : ITTest
{
    // inner quartile values must be enough to produce a valid statistic
    // valid statistics require a minimum of 5 samples
    private const int MinimumSampleSizeForTruncation = 10;

    public TestResults ExecuteTest(double[] before, double[] after, TestSettings settings)
    {
        var sigDig = settings.Round;

        var test = new TwoSampleTTest(
            PreProcessSample(before, settings),
            PreProcessSample(after, settings),
            false);

        var meanBefore = Math.Round(test.EstimatedValue1, sigDig);
        var meanAfter = Math.Round(test.EstimatedValue2, sigDig);

        var medianBefore = Math.Round(before.Median(), sigDig);
        var medianAfter = Math.Round(after.Median(), sigDig);

        var testStatistic = Math.Round(test.Statistic, sigDig);
        var pVal = Math.Round(test.PValue, sigDig);
        var dof = Math.Round(test.DegreesOfFreedom, sigDig);

        var isSignificant = pVal <= settings.Alpha;
        var changeDirection = meanAfter > meanBefore ? SailfishChangeDirection.Regressed : SailfishChangeDirection.Improved;

        var description = isSignificant ? changeDirection : SailfishChangeDirection.NoChange;

        var additionalResults = new Dictionary<string, double>()
        {
            { "DegreesOfFreedom", dof }
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