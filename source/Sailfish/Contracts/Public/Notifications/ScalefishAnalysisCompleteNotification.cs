using System.Collections.Generic;
using MediatR;
using Sailfish.Analysis.ScaleFish;

namespace Sailfish.Contracts.Public.Notifications;

public class ScalefishAnalysisCompleteNotification : INotification
{
    public ScalefishAnalysisCompleteNotification(string scalefishResultMarkdown, List<IScalefishClassModels> testClassComplexityResults)
    {
        ScalefishResultMarkdown = scalefishResultMarkdown;
        TestClassComplexityResults = testClassComplexityResults;
    }

    public List<IScalefishClassModels> TestClassComplexityResults { get; }
    public string ScalefishResultMarkdown { get; }
}