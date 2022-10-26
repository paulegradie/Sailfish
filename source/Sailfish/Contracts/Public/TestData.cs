using System.Collections.Generic;

namespace Sailfish.Contracts.Public;

public class TestData
{
    public IEnumerable<string> TestId { get; }
    public IEnumerable<DescriptiveStatisticsResult> Data { get; }

    public TestData(IEnumerable<string> testId, IEnumerable<DescriptiveStatisticsResult> data)
    {
        TestId = testId;
        Data = data;
    }
}