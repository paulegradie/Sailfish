using System.Collections.Generic;
using System.Threading;
using Sailfish.Execution;

namespace Sailfish.Statistics;

internal interface IExecutionSummaryCompiler
{
    List<ExecutionSummary> CompileToSummaries(List<RawExecutionResult> results, CancellationToken cancellationToken);
}