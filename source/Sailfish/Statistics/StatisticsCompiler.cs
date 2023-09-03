using Sailfish.Analysis;
using Sailfish.Contracts.Public;
using Sailfish.Execution;
using Sailfish.Extensions.Methods;

namespace Sailfish.Statistics;

internal class StatisticsCompiler : IStatisticsCompiler
{ 
    public PerformanceRunResult Compile(TestCaseId testCaseId, PerformanceTimer populatedTimer, IExecutionSettings executionSettings)
    {
        return populatedTimer.ToDescriptiveStatistics(testCaseId, executionSettings);
    }
}