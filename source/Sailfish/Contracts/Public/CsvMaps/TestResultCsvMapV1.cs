using System;
using CsvHelper.Configuration;

namespace Sailfish.Contracts.Public.CsvMaps;

[Obsolete("Please Use TestResultCsvMap instead")]
public sealed class TestResultCsvMapV1 : ClassMap<TestCaseResultsV1>
{
    public TestResultCsvMapV1()
    {
        Map(m => m.DisplayName).Index(0);
        Map(m => m.MeanOfBefore).Index(1);
        Map(m => m.MeanOfAfter).Index(2);
        Map(m => m.PValue).Index(3);
        Map(m => m.DegreesOfFreedom).Index(4);
        Map(m => m.TStatistic).Index(5);
    }
}