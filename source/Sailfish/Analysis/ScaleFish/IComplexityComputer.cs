using System.Collections.Generic;
using Sailfish.Execution;

namespace Sailfish.Analysis.Scalefish;

public interface IComplexityComputer
{
    IEnumerable<ITestClassComplexityResult> AnalyzeComplexity(List<IExecutionSummary> executionSummaries);
}