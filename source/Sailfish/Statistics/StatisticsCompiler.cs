using Sailfish.Execution;

namespace Sailfish.Statistics;

public interface IStatisticsCompiler
{
    TestCaseStatistics Compile(string displayName, PerformanceTimer populatedTimer);
}

public class StatisticsCompiler : IStatisticsCompiler
{
    public TestCaseStatistics Compile(string displayName, PerformanceTimer populatedTimer)
    {
        return populatedTimer.ToTestCaseStatistics(displayName);
    }
}