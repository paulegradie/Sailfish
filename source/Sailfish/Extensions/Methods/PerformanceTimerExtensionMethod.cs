using Sailfish.Analysis;
using Sailfish.Contracts.Public;
using Sailfish.Execution;

namespace Sailfish.Extensions.Methods;

internal static class PerformanceTimerExtensionMethod
{
    public static PerformanceRunResult ToDescriptiveStatistics(this PerformanceTimer populatedPerformanceTimer, TestCaseId testCaseId, IExecutionSettings executionSettings)
    {
        return PerformanceRunResult.ConvertFromPerfTimer(testCaseId, populatedPerformanceTimer, executionSettings);
    }
}