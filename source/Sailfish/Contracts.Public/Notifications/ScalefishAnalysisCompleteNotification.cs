using MediatR;
using Sailfish.Analysis.ScaleFish;
using System.Collections.Generic;

namespace Sailfish.Contracts.Public.Notifications;

public class ScalefishAnalysisCompleteNotification(string scalefishResultMarkdown, List<ScalefishClassModel> testClassComplexityResults) : INotification
{
    public List<ScalefishClassModel> TestClassComplexityResults { get; } = testClassComplexityResults;
    public string ScalefishResultMarkdown { get; } = scalefishResultMarkdown;
}