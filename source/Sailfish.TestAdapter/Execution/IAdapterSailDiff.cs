using System.Collections.Generic;
using System.Threading;
using Sailfish.Analysis.Saildiff;
using Sailfish.Contracts.Public;
using Sailfish.Execution;

namespace Sailfish.TestAdapter.Execution;

internal interface IAdapterSailDiff : ISailDiff
{
    string ComputeTestCaseDiff(
        TestExecutionResult testExecutionResult,
        IExecutionSummary executionSummary,
        TestSettings testSettings,
        IEnumerable<DescriptiveStatisticsResult> preloadedLastRunIfAvailable,
        CancellationToken cancellationToken);
}