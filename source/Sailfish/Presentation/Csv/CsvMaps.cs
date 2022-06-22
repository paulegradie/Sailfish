using CsvHelper.Configuration;
using Sailfish.Statistics;
using Sailfish.Statistics.StatisticalAnalysis;

namespace Sailfish.Presentation.Csv;

public class TestCaseStatisticMap : ClassMap<TestCaseStatistics>
{
    public TestCaseStatisticMap()
    {
        Map(m => m.DisplayName).Index(0);
        Map(m => m.Median).Index(1);
        Map(m => m.Mean).Index(2);
        Map(m => m.StdDev).Index(3);
        Map(m => m.Variance).Index(4);
        Map(m => m.RawExecutionResults).Index(5);
    }
}


public class NamedTTestResultMap : ClassMap<NamedTTestResult>
{
    public NamedTTestResultMap()
    {
        Map(m => m.TestName).Index(0);
        Map(m => m.MeanOfBefore).Index(1);
        Map(m => m.MeanOfAfter).Index(2);
        Map(m => m.PValue).Index(3);
        Map(m => m.DegreesOfFreedom).Index(4);
        Map(m => m.TStatistic).Index(5);
    }
}
