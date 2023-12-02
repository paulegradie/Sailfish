using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Extensions.Methods;

namespace Sailfish.Analysis.SailDiff.Statistics;

internal interface IStatisticsCompiler
{
    PerformanceRunResult Compile(TestCaseId testCaseId, PerformanceTimer populatedTimer, IExecutionSettings executionSettings);
}

internal class StatisticsCompiler : IStatisticsCompiler
{
    public PerformanceRunResult Compile(TestCaseId testCaseId, PerformanceTimer populatedTimer, IExecutionSettings executionSettings)
    {
        return populatedTimer.ToDescriptiveStatistics(testCaseId, executionSettings);
    }
}