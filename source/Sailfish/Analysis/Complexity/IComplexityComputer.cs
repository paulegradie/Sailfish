using System;
using System.Collections.Generic;
using Sailfish.ComplexityEstimation;
using Sailfish.Execution;

namespace Sailfish.Analysis.Complexity;

public interface IComplexityComputer
{
    Dictionary<Type, Dictionary<string, Dictionary<string, ComplexityResult>>> AnalyzeComplexity(List<IExecutionSummary> executionSummaries);
}