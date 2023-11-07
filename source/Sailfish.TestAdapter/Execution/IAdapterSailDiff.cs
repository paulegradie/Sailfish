using System.Threading;
using Sailfish.Analysis.SailDiff;
using Sailfish.Contracts.Public;
using Sailfish.Execution;

namespace Sailfish.TestAdapter.Execution;

internal interface IAdapterSailDiff : ISailDiffInternal
{
    string ComputeTestCaseDiff(
        TestCaseExecutionResult testCaseExecutionResult,
        IClassExecutionSummary classExecutionSummary,
        PerformanceRunResult preloadedLastRun,
        CancellationToken cancellationToken);
}