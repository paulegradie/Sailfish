using System.Collections.Generic;
using MediatR;
using Sailfish.Analysis.ScaleFish;

namespace Sailfish.Contracts.Public.Notifications;

public record ScaleFishAnalysisCompleteNotification : INotification
{
    public ScaleFishAnalysisCompleteNotification(string ScaleFishResultMarkdown,
        List<ScalefishClassModel> TestClassComplexityResults)
    {
        this.ScaleFishResultMarkdown = ScaleFishResultMarkdown;
        this.TestClassComplexityResults = TestClassComplexityResults;
    }

    public string ScaleFishResultMarkdown { get; init; }
    public List<ScalefishClassModel> TestClassComplexityResults { get; init; }

    public void Deconstruct(out string ScaleFishResultMarkdown, out List<ScalefishClassModel> TestClassComplexityResults)
    {
        ScaleFishResultMarkdown = this.ScaleFishResultMarkdown;
        TestClassComplexityResults = this.TestClassComplexityResults;
    }
}