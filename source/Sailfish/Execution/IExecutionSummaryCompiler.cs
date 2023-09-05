using System.Collections.Generic;
using System.Threading;

namespace Sailfish.Execution;

internal interface IExecutionSummaryCompiler
{
    IEnumerable<IClassExecutionSummary> CompileToSummaries(IEnumerable<RawExecutionResult> results, CancellationToken cancellationToken);
}