using Sailfish.Execution;
using Sailfish.Statistics;

namespace Sailfish.ExtensionMethods;

internal static class PerformanceTimerExtensionMethod
{
    public static DescriptiveStatistics ToDescriptiveStatistics(this PerformanceTimer populatedPerformanceTimer, string displayName)
    {
        return new DescriptiveStatistics().ConvertFromPerfTimer(displayName, populatedPerformanceTimer);
    }
}