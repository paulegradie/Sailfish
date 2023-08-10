using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sailfish.Analysis.Saildiff;
using Sailfish.Contracts.Public;
using Sailfish.Execution;

namespace Sailfish.TestAdapter.Execution;

internal interface ITestAdapterExecutionEngine
{
    List<IExecutionSummary> Execute(
        List<TestCase> testCases,
        List<DescriptiveStatisticsResult> preloadedLastRunIfAvailable, 
        TestSettings? testSettings,
        CancellationToken cancellationToken);
}