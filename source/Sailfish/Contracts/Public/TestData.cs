using System.Collections.Generic;

namespace Sailfish.Contracts.Public;

public class TestData
{
    public IEnumerable<string> TestIds { get; }
    public IEnumerable<DescriptiveStatisticsResult> Data { get; }

    public TestData(IEnumerable<string> testIds, IEnumerable<DescriptiveStatisticsResult> data)
    {
        TestIds = testIds;
        Data = data;
    }
}