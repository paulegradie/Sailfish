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

        const int maxArraySize = 10;
        var sample1 = preprocessor.PreprocessWithDownSample(before, settings.UseInnerQuartile, true, maxArraySize);
        var sample2 = preprocessor.PreprocessWithDownSample(after, settings.UseInnerQuartile, true, maxArraySize);
        
        var test = new MannWhitneyWilcoxonTest(sample1, sample2, TwoSampleHypothesis.ValuesAreDifferent);

        var meanBefore = Math.Round(sample1.Mean(), sigDig);
        var meanAfter = Math.Round(sample2.Mean(), sigDig);

        var medianBefore = Math.Round(sample1.Median(), sigDig);
        var medianAfter = Math.Round(sample2.Median(), sigDig);

        var testStatistic = Math.Round(test.Statistic, sigDig);
        var pVal = Math.Round(test.PValue, TestConstants.PValueSigDig);

        var isSignificant = test.PValue <= settings.Alpha;
        var changeDirection = meanAfter > meanBefore ? SailfishChangeDirection.Regressed : SailfishChangeDirection.Improved;

        var description = isSignificant ? changeDirection : SailfishChangeDirection.NoChange;

        var additionalResults = new Dictionary<string, object>
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
            before,
            after,
            additionalResults);
    }
}