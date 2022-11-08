using System.Collections.Generic;
using System.Threading;

namespace Sailfish.Execution;

internal interface IExecutionSummaryCompiler
{
    List<ExecutionSummary> CompileToSummaries(List<RawExecutionResult> results, CancellationToken cancellationToken);
}