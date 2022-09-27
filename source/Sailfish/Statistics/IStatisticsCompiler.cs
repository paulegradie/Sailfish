using Sailfish.Contracts;
using Sailfish.Execution;

namespace Sailfish.Statistics;

internal interface IStatisticsCompiler
{
    DescriptiveStatistics Compile(string displayName, PerformanceTimer populatedTimer);
}