using Sailfish.Execution;

namespace Sailfish.Statistics;

internal interface IStatisticsCompiler
{
    TestCaseStatistics Compile(string displayName, PerformanceTimer populatedTimer);
}