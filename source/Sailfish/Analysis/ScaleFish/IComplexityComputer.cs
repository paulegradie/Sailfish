using System.Collections.Generic;
using Sailfish.Execution;

namespace Sailfish.Analysis.ScaleFish;

public interface IComplexityComputer
{
    IEnumerable<ScalefishClassModel> AnalyzeComplexity(List<IClassExecutionSummary> executionSummaries);
}