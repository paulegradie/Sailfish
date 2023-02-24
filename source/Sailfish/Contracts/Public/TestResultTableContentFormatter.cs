using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using Sailfish.Analysis;
using Sailfish.ExtensionMethods;

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

    private static List<TestCaseResults> FormatAsCsv(List<TestCaseResults> testCaseResults)
    {
        return testCaseResults;
    }

    private static string FormatAsMarkdown(IEnumerable<TestCaseResults> testCaseResults)
    {
        var selectors = new Expression<Func<TestCaseResults, object>>[]
        {
            m => m.TestCaseId.DisplayName,
            m => m.TestResults.MeanOfBefore,
            m => m.TestResults.MeanOfAfter,
            m => m.TestResults.MedianOfBefore,
            m => m.TestResults.MedianOfAfter,
            m => m.TestResults.PValue,
            m => m.TestResults.TestStatistic,
            m => m.TestResults.ChangeDescription
        };

        var headerSuffixes = new[]
        {
            "", "ms", "ms", "ms", "ms", "", "", ""
        };

        var markdownTable = testCaseResults.ToStringTable(headerSuffixes, selectors);
        return markdownTable;
    }
}