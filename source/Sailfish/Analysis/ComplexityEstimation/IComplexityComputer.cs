using System.Collections.Generic;
using Sailfish.Execution;

namespace Sailfish.Analysis.ComplexityEstimation;

public interface IComplexityComputer
{
    IEnumerable<ITestClassComplexityResult> AnalyzeComplexity(List<IExecutionSummary> executionSummaries);
}