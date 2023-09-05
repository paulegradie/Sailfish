using Sailfish.Analysis;
using Sailfish.Contracts.Public;
using Sailfish.Execution;

namespace Sailfish.Statistics;

internal interface IStatisticsCompiler
{
    PerformanceRunResult Compile(TestCaseId testCaseId, PerformanceTimer populatedTimer, IExecutionSettings executionSettings);
}