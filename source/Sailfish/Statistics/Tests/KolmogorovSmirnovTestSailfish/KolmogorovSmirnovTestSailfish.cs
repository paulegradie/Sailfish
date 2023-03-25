using System;
using System.Collections.Generic;
using Accord.Statistics.Testing;
using MathNet.Numerics.Statistics;
using Sailfish.Analysis;
using Sailfish.Contracts;

namespace Sailfish.Statistics.Tests.KolmogorovSmirnovTestSailfish;

public class KolmogorovSmirnovTestSailfish : IKolmogorovSmirnovTestSailfish
{
    private readonly ITestPreprocessor preprocessor;

    public KolmogorovSmirnovTestSailfish(ITestPreprocessor preprocessor)
    {
        this.preprocessor = preprocessor;
    }

    public class AdditionalResults
    {
        public const string EmpiricalDistribution1 = "EmpiricalDistribution1";
        public const string EmpiricalDistribution2 = "EmpiricalDistribution2";
        public const string Significant = "Significant";
        public const string Size = "Size";
        public const string Tail = "Tail";
    }


    public TestResults ExecuteTest(double[] before, double[] after, TestSettings settings)
    {
        var sigDig = settings.Round;

        var sample1 = preprocessor.Preprocess(before, settings.UseInnerQuartile);
        var sample2 = preprocessor.Preprocess(after, settings.UseInnerQuartile);

        var test = new TwoSampleKolmogorovSmirnovTest(sample1, sample2, TwoSampleKolmogorovSmirnovTestHypothesis.SamplesDistributionsAreUnequal);

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
            { AdditionalResults.EmpiricalDistribution1, test.EmpiricalDistribution1 },
            { AdditionalResults.EmpiricalDistribution2, test.EmpiricalDistribution2 },
            { AdditionalResults.Significant, test.Significant },
            { AdditionalResults.Size, test.Size },
            { AdditionalResults.Tail, test.Tail }
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