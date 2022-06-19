using VeerPerforma.Execution;

namespace VeerPerforma.Statistics;

public static class PerformanceTimerExtensionMethod
{
    public static TestCaseStatistics ToTestCaseStatistics(this PerformanceTimer populatedPerformanceTimer)
    {
        return new TestCaseStatistics().ConvertFromPerfTimer(populatedPerformanceTimer);
    }
}