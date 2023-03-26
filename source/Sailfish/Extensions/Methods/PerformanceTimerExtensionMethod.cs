using Sailfish.Analysis;
using Sailfish.Contracts.Public;
using Sailfish.Execution;

namespace Sailfish.Extensions.Methods;

internal static class PerformanceTimerExtensionMethod
{
    public static DescriptiveStatisticsResult ToDescriptiveStatistics(this PerformanceTimer populatedPerformanceTimer, TestCaseId testCaseId)
    {
        return DescriptiveStatisticsResult.ConvertFromPerfTimer(testCaseId, populatedPerformanceTimer);
    }
}