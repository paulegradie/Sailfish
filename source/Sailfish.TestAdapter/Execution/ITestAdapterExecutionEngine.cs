using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sailfish.Analysis.SailDiff;
using Sailfish.Execution;

namespace Sailfish.TestAdapter.Execution;

internal interface ITestAdapterExecutionEngine
{
    Task<List<IExecutionSummary>> Execute(
        List<TestCase> testCases,
        List<List<IExecutionSummary>> preloadedLastRunIfAvailable, 
        TestSettings? testSettings,
        CancellationToken cancellationToken);
}