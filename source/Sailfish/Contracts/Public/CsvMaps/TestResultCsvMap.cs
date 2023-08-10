using CsvHelper.Configuration;

namespace Sailfish.Contracts.Public.CsvMaps;

public sealed class TestResultCsvMap : ClassMap<TestCaseResults>
{
    public TestResultCsvMap()
    {
        Map(m => m.TestCaseId.DisplayName).Index(0);
        Map(m => m.TestResults.MeanBefore).Index(1);
        Map(m => m.TestResults.MeanAfter).Index(2);
        Map(m => m.TestResults.MedianBefore).Index(3);
        Map(m => m.TestResults.MedianAfter).Index(4);
        Map(m => m.TestResults.PValue).Index(5);
        Map(m => m.TestResults.TestStatistic).Index(6);
        Map(m => m.TestResults.SampleSizeBefore).Index(7);
        Map(m => m.TestResults.SampleSizeAfter).Index(8);
    }
}