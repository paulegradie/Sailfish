using System.Collections.Generic;
using Sailfish.Contracts.Public;

namespace Sailfish.Analysis;

public class TestResultFormats
{
    public TestResultFormats(string markdownFormat, IEnumerable<TestCaseResults> csvFormat, TestIds testIds)
    {
        MarkdownFormat = markdownFormat;
        CsvFormat = csvFormat;
        TestIds = testIds;
    }

    public string MarkdownFormat { get; set; }
    public IEnumerable<TestCaseResults> CsvFormat { get; set; }
    public TestIds TestIds { get; set; }
}