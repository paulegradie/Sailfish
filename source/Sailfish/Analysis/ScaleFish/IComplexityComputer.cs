using System.Collections.Generic;
using Sailfish.Execution;

namespace Sailfish.Analysis.ScaleFish;

public interface IComplexityComputer
{
    IEnumerable<ITestClassComplexityResult> AnalyzeComplexity(List<IExecutionSummary> executionSummaries);
}