using Sailfish.Execution;
using Sailfish.ExtensionMethods;

namespace Sailfish.Statistics;

internal class StatisticsCompiler : IStatisticsCompiler
{
    public TestCaseStatistics Compile(string displayName, PerformanceTimer populatedTimer)
    {
        return populatedTimer.ToTestCaseStatistics(displayName);
    }
}