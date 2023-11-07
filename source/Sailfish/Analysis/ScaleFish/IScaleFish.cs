using Sailfish.Contracts.Serialization.V1;
using Sailfish.Presentation;

namespace Sailfish.Analysis.ScaleFish;

internal interface IScaleFishInternal : IAnalyzeFromFile
{
}

public interface IScaleFish
{
    void Analyze(ClassExecutionSummaryTrackingFormat summaryTrackingFormat);
}