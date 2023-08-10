using System;
using System.Collections.Generic;
using Accord.Statistics;
using Accord.Statistics.Testing;
using Sailfish.Analysis.Saildiff;
using Sailfish.Contracts;

namespace Sailfish.Statistics.Tests.TTestSailfish;

internal class TTestSailfish : ITTestSailfish
{
    private readonly ITestPreprocessor preprocessor;

    public TTestSailfish(ITestPreprocessor preprocessor)
    {
        this.preprocessor = preprocessor;
    }

    public class AdditionalResults
    {
        public const string DegreesOfFreedom = "DegreesOfFreedom";
    }

    public TestResults ExecuteTest(double[] before, double[] after, TestSettings settings)
    {
        var sigDig = settings.Round;

        var sample1 = preprocessor.Preprocess(before, settings.UseInnerQuartile);
        var sample2 = preprocessor.Preprocess(after, settings.UseInnerQuartile);

        var test = new TwoSampleTTest(sample1, sample2, false);

        var meanBefore = Math.Round(test.EstimatedValue1, sigDig);
        var meanAfter = Math.Round(test.EstimatedValue2, sigDig);

        var medianBefore = Math.Round(sample1.Median(), sigDig);
        var medianAfter = Math.Round(sample2.Median(), sigDig);

        var testStatistic = Math.Round(test.Statistic, sigDig);
        var dof = Math.Round(test.DegreesOfFreedom, sigDig);

        var isSignificant = test.PValue <= settings.Alpha;
        var pVal = Math.Round(test.PValue, TestConstants.PValueSigDig);
        var changeDirection = meanAfter > meanBefore ? SailfishChangeDirection.Regressed : SailfishChangeDirection.Improved;

        var description = isSignificant ? changeDirection : SailfishChangeDirection.NoChange;

        var additionalResults = new Dictionary<string, object>()
        {
            { AdditionalResults.DegreesOfFreedom, dof }
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
            before,
            after,
            additionalResults);
    }
}