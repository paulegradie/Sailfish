using System;
using System.Collections.Generic;
using Accord.Statistics.Testing;
using MathNet.Numerics.Statistics;
using Sailfish.Analysis.SailDiff;
using Sailfish.Contracts;
using Sailfish.Contracts.Public;

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


    public TestResultWithOutlierAnalysis ExecuteTest(double[] before, double[] after, SailDiffSettings settings)
    {
        var sigDig = settings.Round;

        try
        {
            var preprocessed1 = preprocessor.Preprocess(before, settings.UseOutlierDetection);
            var preprocessed2 = preprocessor.Preprocess(after, settings.UseOutlierDetection);

            var sample1 = preprocessed1.OutlierAnalysis?.DataWithOutliersRemoved ?? preprocessed1.RawData;
            var sample2 = preprocessed2.OutlierAnalysis?.DataWithOutliersRemoved ?? preprocessed2.RawData;

            var test = new TwoSampleKolmogorovSmirnovTest(
                sample1,
                sample2,
                TwoSampleKolmogorovSmirnovTestHypothesis.SamplesDistributionsAreUnequal);

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

            var testResults = new TestResults(
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
            return new TestResultWithOutlierAnalysis(testResults, preprocessed1.OutlierAnalysis, preprocessed2.OutlierAnalysis);
        }
        catch (Exception ex)
        {
            return new TestResultWithOutlierAnalysis(ex);
        }
    }
}