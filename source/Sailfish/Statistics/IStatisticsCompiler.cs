using Sailfish.Contracts;
using Sailfish.Contracts.Public;
using Sailfish.Execution;

namespace Sailfish.Statistics;

internal interface IStatisticsCompiler
{
    DescriptiveStatisticsResult Compile(string displayName, PerformanceTimer populatedTimer);
}