using CsvHelper.Configuration;
using Sailfish.Contracts.Private.CsvMaps.Converters;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Contracts.Private.CsvMaps;

internal sealed class SailDiffWriteAsCsvMap : ClassMap<SailDiffResult>
{
    public SailDiffWriteAsCsvMap()
    {
        Map(m => m.TestCaseId.DisplayName).Index(0);
        Map(m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.MeanBefore).Index(1);
        Map(m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.MeanAfter).Index(2);
        Map(m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.MedianBefore).Index(3);
        Map(m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.MedianAfter).Index(4);
        Map(m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.TestStatistic).Index(5);
        Map(m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.PValue).Index(6);
        Map(m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.ChangeDescription).Index(7);
        Map(m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.SampleSizeBefore).Index(8);
        Map(m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.SampleSizeAfter).Index(9);
        Map(m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.RawDataBefore).TypeConverter<DoubleArrayCsvConverter>().Index(10);
        Map(m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.RawDataAfter).TypeConverter<DoubleArrayCsvConverter>().Index(11);
    }
}