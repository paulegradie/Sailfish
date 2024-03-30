using System.Collections.Generic;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Contracts.Public;

public class TestData(IEnumerable<string> testIds, IEnumerable<PerformanceRunResult> data)
{
    public IEnumerable<string> TestIds { get; } = testIds;
    public IEnumerable<PerformanceRunResult> Data { get; } = data;
}