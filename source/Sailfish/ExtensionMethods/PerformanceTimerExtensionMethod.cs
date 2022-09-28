using Sailfish.Contracts;
using Sailfish.Contracts.Public;
using Sailfish.Execution;

namespace Sailfish.ExtensionMethods;

internal static class PerformanceTimerExtensionMethod
{
    public static DescriptiveStatisticsResult ToDescriptiveStatistics(this PerformanceTimer populatedPerformanceTimer, string displayName)
    {
        return DescriptiveStatisticsResult.ConvertFromPerfTimer(displayName, populatedPerformanceTimer);
    }
}