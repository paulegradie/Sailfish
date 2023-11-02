using CsvHelper.Configuration;
using Sailfish.Contracts.Public;

namespace Sailfish.Contracts.Private.CsvMaps;

internal sealed class WriteAsCsvMap : ClassMap<PerformanceRunResult>
{
    public WriteAsCsvMap()
    {
        Map(m => m.DisplayName).Index(0);
        Map(m => m.Median).Index(1);
        Map(m => m.Mean).Index(2);
        Map(m => m.StdDev).Index(3);
        Map(m => m.Variance).Index(4);
        Map(m => m.LowerOutliers).Index(5);
        Map(m => m.UpperOutliers).Index(6);
        Map(m => m.TotalNumOutliers).Index(7);
        Map(m => m.SampleSize).Index(8);
        Map(m => m.RawExecutionResults).TypeConverter<DoubleArrayCsvConverter>().Index(9);
    }
}