using Sailfish.Analysis;
using Sailfish.Contracts.Public;
using Sailfish.Execution;
using Sailfish.ExtensionMethods;

namespace Sailfish.Statistics;

internal class StatisticsCompiler : IStatisticsCompiler
{
    public DescriptiveStatisticsResult Compile(TestCaseId testCaseId, PerformanceTimer populatedTimer)
    {
        return populatedTimer.ToDescriptiveStatistics(testCaseId);
    }
}