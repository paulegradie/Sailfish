using System.Collections.Generic;
using Sailfish.Execution;

namespace Sailfish.Statistics;

internal interface IExecutionSummaryCompiler
{
    List<ExecutionSummary> CompileToSummaries(List<RawExecutionResult> results);
}