using System.Collections.Generic;

namespace Sailfish.Contracts.Public;

public class TestData
{
    public IEnumerable<string> TestId { get; }
    public List<DescriptiveStatisticsResult> Data { get; }

    public TestData(IEnumerable<string> testId, List<DescriptiveStatisticsResult> data)
    {
        TestId = testId;
        Data = data;
    }
}