using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using Sailfish.Analysis;
using Sailfish.Extensions.Methods;

namespace Sailfish.Contracts.Public;

public interface ITestResultTableContentFormatter
{
    TestResultFormats CreateTableFormats(List<TestCaseResults> testCaseResults, TestIds testIds, CancellationToken cancellationToken);
}

public class TestResultTableContentFormatter : ITestResultTableContentFormatter
{
    public TestResultFormats CreateTableFormats(List<TestCaseResults> testCaseResults, TestIds testIds, CancellationToken cancellationToken)
    {
        var markdownFormat = FormatAsMarkdown(testCaseResults);
        var csvFormat = FormatAsCsv(testCaseResults);

        return new TestResultFormats(markdownFormat, csvFormat, testIds);
    }

    private static IEnumerable<TestCaseResults> FormatAsCsv(IEnumerable<TestCaseResults> testCaseResults)
    {
        return testCaseResults;
    }

    private static string FormatAsMarkdown(IEnumerable<TestCaseResults> testCaseResults)
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

        return testCaseResults.ToStringTable(headerSuffixes, selectors);
    }
}