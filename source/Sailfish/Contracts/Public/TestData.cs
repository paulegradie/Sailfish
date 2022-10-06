using System.Collections.Generic;

namespace Sailfish.Contracts.Public;

public class TestData
{
    public string TestId { get; }
    public List<DescriptiveStatisticsResult> Data { get; }

    public TestData(string testId, List<DescriptiveStatisticsResult> data)
    {
        TestId = testId;
        Data = data;
    }
}