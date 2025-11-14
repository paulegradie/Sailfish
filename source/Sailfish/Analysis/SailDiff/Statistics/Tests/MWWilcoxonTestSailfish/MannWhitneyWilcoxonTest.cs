using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MathNet.Numerics.Statistics;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers.Factories;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Analysis.SailDiff.Statistics.Tests.MWWilcoxonTestSailfish;

public interface IMannWhitneyWilcoxonTest : ITest;

public class MannWhitneyWilcoxonTest : IMannWhitneyWilcoxonTest
{
    private const int MaxArraySize = 10;
    private readonly ITestPreprocessor _preprocessor;

    public MannWhitneyWilcoxonTest(ITestPreprocessor preprocessor)
    {
        this._preprocessor = preprocessor;
    }

    public TestResultWithOutlierAnalysis ExecuteTest(double[] before, double[] after, SailDiffSettings settings)
    {
        var sigDig = settings.Round;

        var iterations = before.Length + after.Length > 20 ? 25 : 1;
        var tests = new ConcurrentBag<MannWhitneyWilcoxon>();

        try
        {
            Parallel.ForEach(
                Enumerable.Range(0, iterations),
                new ParallelOptions { MaxDegreeOfParallelism = 5 },
                _ =>
                {
                    var (p1, p2) = _preprocessor.PreprocessJointlyWithDownSample(before, after, settings.UseOutlierDetection, maxArraySize: MaxArraySize);

                    var sample1 = p1.OutlierAnalysis?.DataWithOutliersRemoved ?? p1.RawData;
                    var sample2 = p2.OutlierAnalysis?.DataWithOutliersRemoved ?? p2.RawData;

                    var test = MannWhitneyWilcoxonFactory.Create(sample1, sample2);
                    tests.Add(test);
                });
            var meanBefore = Math.Round(before.Mean(), sigDig);
            var meanAfter = Math.Round(after.Mean(), sigDig);
            var medianBefore = Math.Round(before.Median(), sigDig);
            var medianAfter = Math.Round(after.Median(), sigDig);
            var testStatistic = Math.Round(tests.Select(x => x.Statistic).Mean(), sigDig);
            var significantPValues = tests
                .Select(x => x.PValue)
                .Where(p => p < settings.Alpha)
                .ToList();
            var isSignificant = significantPValues.Count / (double)tests.Count > 0.5;
            var pVal = Math.Round(isSignificant
                ? significantPValues.Mean()
                : tests
                    .Select(x => x.PValue)
                    .Where(p => p > settings.Alpha).Mean(), TestConstants.PValueSigDig);
            var description = isSignificant
                ? meanAfter > meanBefore ? SailfishChangeDirection.Regressed : SailfishChangeDirection.Improved
                : SailfishChangeDirection.NoChange;
            var additionalResults = new Dictionary<string, object>
            {
                { AdditionalResults.Statistic1, tests.Select(x => x.Statistic1).Mean() }, { AdditionalResults.Statistic2, tests.Select(x => x.Statistic2).Mean() }
            };

            var testResults = new StatisticalTestResult(
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

            var (rep1, rep2) = _preprocessor.PreprocessJointlyWithDownSample(before, after, settings.UseOutlierDetection, maxArraySize: MaxArraySize);
            return new TestResultWithOutlierAnalysis(testResults, rep1.OutlierAnalysis, rep2.OutlierAnalysis);
        }
        catch (Exception ex)
        {
            return new TestResultWithOutlierAnalysis(ex);
        }
    }

    public record AdditionalResults
    {
        public const string Statistic1 = "Statistic1";
        public const string Statistic2 = "Statistic2";
    }
}