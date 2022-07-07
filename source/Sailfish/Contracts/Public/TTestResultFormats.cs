using System.Collections.Generic;

namespace Sailfish.Contracts.Public;

public class TTestResultFormats
{
    public TTestResultFormats(string markdownTable, List<NamedTTestResult> csvRows)
    {
        MarkdownTable = markdownTable;
        CsvRows = csvRows;
    }

    public string MarkdownTable { get; set; }
    public List<NamedTTestResult> CsvRows { get; set; }
}