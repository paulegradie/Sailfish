using System;
using Accord.Statistics.Testing;

namespace VeerPerforma.Statistics.StatisticalAnalysis;

public class TTest : ITTest
{
    private readonly int sigDig = 3;

    public TTestResult ExecuteTest(double[] before, double[] after, double alpha = 0.01)
    {
        var test = new TwoSampleTTest(before, after, false);

        var meanBefore = Math.Round(test.EstimatedValue1, sigDig);
        var meanAfter = Math.Round(test.EstimatedValue2, sigDig);
        var testStatistic = Math.Round(test.Statistic, sigDig);
        var pVal = Math.Round(test.PValue, sigDig);
        var dof = Math.Round(test.DegreesOfFreedom, sigDig);

        var isSignificant = pVal <= alpha;
        var changeDirection = meanAfter > meanBefore ? "Regressed" : "Improved";

        var description = isSignificant ? changeDirection : "No change";

        return new TTestResult(
            meanBefore,
            meanAfter,
            testStatistic,
            pVal,
            dof,
            description);
    }
}