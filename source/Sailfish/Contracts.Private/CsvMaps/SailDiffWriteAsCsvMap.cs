using CsvHelper.Configuration;
using Sailfish.Contracts.Public;

namespace Sailfish.Contracts.Private.CsvMaps;

internal sealed class SailDiffWriteAsCsvMap : ClassMap<TestCaseResults>
{
    public SailDiffWriteAsCsvMap()
    {
        Map(m => m.TestCaseId.DisplayName).Index(0);
        Map(m => m.TestResultsWithOutlierAnalysis.TestResults.MeanBefore).Index(1);
        Map(m => m.TestResultsWithOutlierAnalysis.TestResults.MeanAfter).Index(2);
        Map(m => m.TestResultsWithOutlierAnalysis.TestResults.MedianBefore).Index(3);
        Map(m => m.TestResultsWithOutlierAnalysis.TestResults.MedianAfter).Index(4);
        Map(m => m.TestResultsWithOutlierAnalysis.TestResults.TestStatistic).Index(5);
        Map(m => m.TestResultsWithOutlierAnalysis.TestResults.PValue).Index(6);
        Map(m => m.TestResultsWithOutlierAnalysis.TestResults.ChangeDescription).Index(7);
        Map(m => m.TestResultsWithOutlierAnalysis.TestResults.SampleSizeBefore).Index(8);
        Map(m => m.TestResultsWithOutlierAnalysis.TestResults.SampleSizeAfter).Index(9);
        Map(m => m.TestResultsWithOutlierAnalysis.TestResults.RawDataBefore).TypeConverter<DoubleArrayCsvConverter>().Index(10);
        Map(m => m.TestResultsWithOutlierAnalysis.TestResults.RawDataAfter).TypeConverter<DoubleArrayCsvConverter>().Index(11);
    }
}