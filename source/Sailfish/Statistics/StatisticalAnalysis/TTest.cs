using System;
using Accord.Statistics.Testing;
using Sailfish.Presentation.TTest;

namespace Sailfish.Statistics.StatisticalAnalysis;

internal class TTest : ITTest
{
    public TTestResult ExecuteTest(double[] before, double[] after, TTestSettings settings)
    {
        var sigDig = settings.Round;
        var test = new TwoSampleTTest(before, after, false);

        var meanBefore = Math.Round(test.EstimatedValue1, sigDig);
        var meanAfter = Math.Round(test.EstimatedValue2, sigDig);
        var testStatistic = Math.Round(test.Statistic, sigDig);
        var pVal = Math.Round(test.PValue, sigDig);
        var dof = Math.Round(test.DegreesOfFreedom, sigDig);

        var isSignificant = pVal <= settings.Alpha;
        var changeDirection = meanAfter > meanBefore ? "*Regressed" : "*Improved";

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