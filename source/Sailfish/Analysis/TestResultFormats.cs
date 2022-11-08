using System.Collections.Generic;
using Sailfish.Contracts.Public;

namespace Sailfish.Analysis;

public class TestResultFormats
{
    public TestResultFormats(string markdownFormat, List<TestCaseResults> csvFormat, TestIds testIds)
    {
        MarkdownFormat = markdownFormat;
        CsvFormat = csvFormat;
        TestIds = testIds;
    }

    public string MarkdownFormat { get; set; }
    public List<TestCaseResults> CsvFormat { get; set; }
    public TestIds TestIds { get; set; }
}