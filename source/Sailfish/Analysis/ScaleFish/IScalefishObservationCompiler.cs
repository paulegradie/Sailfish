using Sailfish.Execution;

namespace Sailfish.Analysis.ScaleFish;

public interface IScalefishObservationCompiler
{
    ObservationSetFromSummaries? CompileObservationSet(IClassExecutionSummary testClassSummary);
}