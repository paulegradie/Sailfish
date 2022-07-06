using CsvHelper.Configuration;

namespace Sailfish.Contracts.Public.CsvMaps;

public sealed class NamedTTestResultMap : ClassMap<NamedTTestResult>
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