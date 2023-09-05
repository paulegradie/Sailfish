using System.Collections.Generic;
using Sailfish.Execution;

namespace Sailfish.Analysis.ScaleFish;

public interface IComplexityComputer
{
    IEnumerable<IScalefishClassModels> AnalyzeComplexity(List<IClassExecutionSummary> executionSummaries);
}