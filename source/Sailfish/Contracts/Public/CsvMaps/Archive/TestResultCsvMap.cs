using CsvHelper.Configuration;

namespace Sailfish.Contracts.Public.CsvMaps.Archive;

public sealed class TestResultCsvMap : ClassMap<TestCaseResults>
{
    public TestResultCsvMap()
    {
        Map(m => m.TestCaseId.DisplayName).Index(0);
        Map(m => m.TestResultsWithOutlierAnalysis.TestResults.MeanBefore).Index(1);
        Map(m => m.TestResultsWithOutlierAnalysis.TestResults.MeanAfter).Index(2);
        Map(m => m.TestResultsWithOutlierAnalysis.TestResults.MedianBefore).Index(3);
        Map(m => m.TestResultsWithOutlierAnalysis.TestResults.MedianAfter).Index(4);
        Map(m => m.TestResultsWithOutlierAnalysis.TestResults.PValue).Index(5);
        Map(m => m.TestResultsWithOutlierAnalysis.TestResults.TestStatistic).Index(6);
        Map(m => m.TestResultsWithOutlierAnalysis.TestResults.SampleSizeBefore).Index(7);
        Map(m => m.TestResultsWithOutlierAnalysis.TestResults.SampleSizeAfter).Index(8);
        Map(m => m.TestResultsWithOutlierAnalysis.ExceptionMessage).Index(9);
    }
}