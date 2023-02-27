using System;
using CsvHelper.Configuration;

namespace Sailfish.Contracts.Public.CsvMaps;

[Obsolete("Please use DescriptiveStatisticsResultCsvMap instead")]
public sealed class TestCaseDescriptiveStatisticsMap : ClassMap<DescriptiveStatisticsResult>
{
    public TestCaseDescriptiveStatisticsMap()
    {
        Map(m => m.DisplayName).Index(0);
        Map(m => m.Median).Index(1);
        Map(m => m.Mean).Index(2);
        Map(m => m.StdDev).Index(3);
        Map(m => m.Variance).Index(4);
        Map(m => m.RawExecutionResults).Index(5);
    }
}

public sealed class DescriptiveStatisticsResultCsvMap : ClassMap<DescriptiveStatisticsResult>
{
    public DescriptiveStatisticsResultCsvMap()
    {
        Map(m => m.DisplayName).Index(0);
        Map(m => m.Median).Index(1);
        Map(m => m.Mean).Index(2);
        Map(m => m.StdDev).Index(3);
        Map(m => m.Variance).Index(4);
        Map(m => m.RawExecutionResults).Index(5);
    }
}