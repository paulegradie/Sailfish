using System.Collections.Generic;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Contracts.Public;

public class TestData
{
    public TestData(IEnumerable<string> testIds, IEnumerable<PerformanceRunResult> data)
    {
        TestIds = testIds;
        Data = data;
    }

    public IEnumerable<string> TestIds { get; }
    public IEnumerable<PerformanceRunResult> Data { get; }
}