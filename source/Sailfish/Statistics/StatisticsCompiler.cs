using Sailfish.Execution;
using Sailfish.ExtensionMethods;

namespace Sailfish.Statistics;

internal class StatisticsCompiler : IStatisticsCompiler
{
    public DescriptiveStatistics Compile(string displayName, PerformanceTimer populatedTimer)
    {
        return populatedTimer.ToDescriptiveStatistics(displayName);
    }
}