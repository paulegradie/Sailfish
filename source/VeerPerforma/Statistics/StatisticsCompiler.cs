using VeerPerforma.Execution;

namespace VeerPerforma.Statistics;

public interface IStatisticsCompiler
{
    TestCaseStatistics Compile(PerformanceTimer populatedTimer);
}

public class StatisticsCompiler : IStatisticsCompiler
{
    public TestCaseStatistics Compile(PerformanceTimer populatedTimer)
    {
        return populatedTimer.ToTestCaseStatistics();
    }
}