using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sailfish.Analysis.SailDiff;
using Sailfish.Execution;
using Sailfish.Extensions.Types;

namespace Sailfish.TestAdapter.Execution;

internal interface ITestAdapterExecutionEngine
{
    Task<List<IClassExecutionSummary>> Execute(
        List<TestCase> testCases,
        TrackingFileDataList preloadedLastRunIfAvailable, 
        SailDiffSettings? testSettings,
        CancellationToken cancellationToken);
}