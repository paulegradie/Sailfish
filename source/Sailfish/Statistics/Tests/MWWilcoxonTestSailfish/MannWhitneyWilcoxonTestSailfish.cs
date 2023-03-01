using System;
using System.Collections.Generic;
using Accord.Statistics;
using Accord.Statistics.Testing;
using Sailfish.Analysis;
using Sailfish.Contracts;

namespace Sailfish.Statistics.Tests.MWWilcoxonTestSailfish;

public class MannWhitneyWilcoxonTestSailfish : IMannWhitneyWilcoxonTestSailfish
{
    private readonly ITestPreprocessor preprocessor;

    public MannWhitneyWilcoxonTestSailfish(ITestPreprocessor preprocessor)
    {
        this.preprocessor = preprocessor;
    }

    public class AdditionalResults
    {
        public const string Statistic1 = "Statistic1";
        public const string Statistic2 = "Statistic2";
    }

    public TestResults ExecuteTest(double[] before, double[] after, TestSettings settings)
    {
        var sigDig = settings.Round;

        var sample1 = preprocessor.Preprocess(before, settings.UseInnerQuartile);
        var sample2 = preprocessor.Preprocess(after, settings.UseInnerQuartile);

        var test = new MannWhitneyWilcoxonTest(sample1, sample2, TwoSampleHypothesis.ValuesAreDifferent);

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
}