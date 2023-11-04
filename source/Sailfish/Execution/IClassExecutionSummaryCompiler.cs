using System.Collections.Generic;
using System.Threading;

namespace Sailfish.Execution;

internal interface IClassExecutionSummaryCompiler
{
    IEnumerable<IClassExecutionSummary> CompileToSummaries(IEnumerable<TestClassResultGroup> results);
}