using System.Collections.Generic;

namespace Sailfish.Execution;

internal interface IClassExecutionSummaryCompiler
{
    IEnumerable<IClassExecutionSummary> CompileToSummaries(IEnumerable<TestClassResultGroup> results);
}