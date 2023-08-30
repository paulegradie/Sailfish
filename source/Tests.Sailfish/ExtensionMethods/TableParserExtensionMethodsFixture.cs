using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Sailfish.Analysis;
using Sailfish.Analysis.Saildiff;
using Sailfish.Contracts.Public;
using Sailfish.Extensions.Methods;
using Sailfish.Statistics.Tests;
using Sailfish.Statistics.Tests.KolmogorovSmirnovTestSailfish;
using Sailfish.Statistics.Tests.MWWilcoxonTestSailfish;
using Sailfish.Statistics.Tests.TTestSailfish;
using Sailfish.Statistics.Tests.TwoSampleWilcoxonSignedRankTestSailfish;
using Shouldly;
using Xunit;

namespace Test.ExtensionMethods;

public class TableParserExtensionMethodsFixture
{
    [Fact]
    public void TableIsParsedCorrectly()
    {
        var selectors = new Expression<Func<TestCaseResults, object>>[]
        {
            m => m.TestCaseId.DisplayName,
            m => m.TestResults.MeanBefore,
            m => m.TestResults.MeanAfter,
            m => m.TestResults.MedianBefore,
            m => m.TestResults.MedianAfter,
            m => m.TestResults.PValue,
            m => m.TestResults.TestStatistic,
            m => m.TestResults.ChangeDescription
        };

        var headerSuffixes = new[]
        {
            "", "ms", "ms", "ms", "ms", "", "", ""
        };

        var preprocessor = new TestPreprocessor();

        var result = new StatisticalTestExecutor(
            new MannWhitneyWilcoxonTestSailfish(preprocessor),
            new TTestSailfish(preprocessor),
            new TwoSampleWilcoxonSignedRankTestSailfish(preprocessor),
            new KolmogorovSmirnovTestSailfish(preprocessor)
        ).ExecuteStatisticalTest(
            new double[] { 2, 3, 4, 4, 5, 5, 6, 6, 6 },
            new double[] { 9, 8, 7, 6, 4, 4, 1, 2, 3, 2 },
            new TestSettings(0.01, 0, false, TestType.WilcoxonRankSumTest));

        var testCaseId = new TestCaseId("MyClass.MySampleTest(N: 2, X: 4)");
        var testCaseResults = new List<TestCaseResults>() { new TestCaseResults(testCaseId, result) };

        var res = testCaseResults
            .ToStringTable(headerSuffixes, selectors)
            .Trim()
            .Split(Environment.NewLine)
            .Select(x => x.Trim())
            .ToArray();

        var expected = new[]
        {
            "| DisplayName                      | MeanBefore | MeanAfter | MedianBefore | MedianAfter |       PValue | TestStatistic | ChangeDescription |",
            "| ---                              | ---        | ---       | ---          | ---         | ---          | ---           | ---               |",
            "| MyClass.MySampleTest(N: 2, X: 4) |       5 ms |      5 ms |         5 ms |        4 ms | 0.8896057503 |            47 |         No Change |"
        };
        res.ShouldBeEquivalentTo(expected);
    }
}