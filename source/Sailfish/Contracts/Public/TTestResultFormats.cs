using System.Collections.Generic;
using Sailfish.Presentation.TTest;

namespace Sailfish.Contracts.Public;

public class TestResultFormats
{
    public TestResultFormats(string markdownTable, List<NamedTTestResult> csvRows, TestIds testIds)
    {
        MarkdownTable = markdownTable;
        CsvRows = csvRows;
        TestIds = testIds;
    }

    public string MarkdownTable { get; set; }
    public List<NamedTTestResult> CsvRows { get; set; }
    public TestIds TestIds { get; set; }
}