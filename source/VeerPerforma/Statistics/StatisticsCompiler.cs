using VeerPerforma.Execution;

namespace VeerPerforma.Statistics;

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