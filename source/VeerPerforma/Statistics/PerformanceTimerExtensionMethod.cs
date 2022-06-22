using VeerPerforma.Execution;

namespace VeerPerforma.Statistics;

public static class PerformanceTimerExtensionMethod
{
    public static TestCaseStatistics ToTestCaseStatistics(this PerformanceTimer populatedPerformanceTimer, string displayName)
    {
        return new TestCaseStatistics().ConvertFromPerfTimer(displayName, populatedPerformanceTimer);
    }
}