using System;
using Accord.Statistics.Testing;
using Sailfish.Contracts;
using Sailfish.Presentation.TTest;
using Sailfish.Utils.MathOps;

namespace Sailfish.Statistics.StatisticalAnalysis;

internal class TTest : ITTest
{
    // inner quartile values must be enough to produce a valid statistic
    // valid statistics require a minimum of 5 samples
    private const int MinimumSampleSizeForTruncation = 10;

    public TTestResult ExecuteTest(double[] before, double[] after, TestSettings settings)
    {
        var sigDig = settings.Round;

        var test = new TwoSampleTTest(
            PreProcessSample(before, settings),
            PreProcessSample(after, settings),
            false);

        var meanBefore = Math.Round(test.EstimatedValue1, sigDig);
        var meanAfter = Math.Round(test.EstimatedValue2, sigDig);
        var testStatistic = Math.Round(test.Statistic, sigDig);
        var pVal = Math.Round(test.PValue, sigDig);
        var dof = Math.Round(test.DegreesOfFreedom, sigDig);

        var isSignificant = pVal <= settings.Alpha;
        var changeDirection = meanAfter > meanBefore ? SailfishChangeDirection.Regressed : SailfishChangeDirection.Improved;

        var description = isSignificant ? changeDirection : SailfishChangeDirection.NoChange;

        return new TTestResult(
            meanBefore,
            meanAfter,
            testStatistic,
            pVal,
            dof,
            description);
    }

    private static double[] PreProcessSample(double[] rawData, TestSettings settings)
    {
        if (!settings.UseInnerQuartile) return rawData;
        if (rawData.Length < MinimumSampleSizeForTruncation) return rawData;
        var quartiles = ComputeQuartiles.GetInnerQuartileValues(rawData);
        return quartiles;
    }
}