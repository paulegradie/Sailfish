using Sailfish.Analysis;
using Sailfish.Contracts.Public;
using Sailfish.Execution;

namespace Sailfish.Statistics;

internal interface IStatisticsCompiler
{
    DescriptiveStatisticsResult Compile(TestCaseId testCaseId, PerformanceTimer populatedTimer);
}