using Sailfish.Execution;
using Sailfish.Statistics;

namespace Sailfish.ExtensionMethods;

internal static class PerformanceTimerExtensionMethod
{
    public static TestCaseStatistics ToTestCaseStatistics(this PerformanceTimer populatedPerformanceTimer, string displayName)
    {
        return new TestCaseStatistics().ConvertFromPerfTimer(displayName, populatedPerformanceTimer);
    }
}