using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Accord.Statistics;
using Accord.Statistics.Testing;
using MathNet.Numerics.Statistics;
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

        var iterations = before.Length + after.Length > 20 ? 50 : 1;
        var tests = new ConcurrentBag<MannWhitneyWilcoxonTest>();

        // bootstrap analysis
        Parallel.ForEach(
            Enumerable.Range(0, iterations),
            new ParallelOptions()
            {
                MaxDegreeOfParallelism = 5
            }, (_) =>
            {
                var sample1 = preprocessor.PreprocessWithDownSample(before, settings.UseInnerQuartile, maxArraySize: maxArraySize);
                var sample2 = preprocessor.PreprocessWithDownSample(after, settings.UseInnerQuartile, maxArraySize: maxArraySize);

                var test = new MannWhitneyWilcoxonTest(sample1, sample2, TwoSampleHypothesis.ValuesAreDifferent);
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
            ? (meanAfter > meanBefore ? SailfishChangeDirection.Regressed : SailfishChangeDirection.Improved)
            : SailfishChangeDirection.NoChange;

        var additionalResults = new Dictionary<string, object>
        {
            { AdditionalResults.Statistic1, tests.Select(x => x.Statistic1).Mean() },
            { AdditionalResults.Statistic2, tests.Select(x => x.Statistic2).Mean() }
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