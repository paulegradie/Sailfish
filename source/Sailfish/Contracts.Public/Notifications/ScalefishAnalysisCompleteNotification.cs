using System.Collections.Generic;
using MediatR;
using Sailfish.Analysis.ScaleFish;

namespace Sailfish.Contracts.Public.Notifications;

public class ScalefishAnalysisCompleteNotification : INotification
{
    public ScalefishAnalysisCompleteNotification(string scalefishResultMarkdown, List<ScalefishClassModel> testClassComplexityResults)
    {
        ScalefishResultMarkdown = scalefishResultMarkdown;
        TestClassComplexityResults = testClassComplexityResults;
    }

    public List<ScalefishClassModel> TestClassComplexityResults { get; }
    public string ScalefishResultMarkdown { get; }
}