using System.Collections.Generic;
using MediatR;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Contracts.Public.Notifications;

public record SailDiffAnalysisCompleteNotification : INotification
{
    public SailDiffAnalysisCompleteNotification(IEnumerable<SailDiffResult> TestCaseResults,
        string ResultsAsMarkdown)
    {
        this.TestCaseResults = TestCaseResults;
        this.ResultsAsMarkdown = ResultsAsMarkdown;
    }

    public IEnumerable<SailDiffResult> TestCaseResults { get; init; }
    public string ResultsAsMarkdown { get; init; }

    public void Deconstruct(out IEnumerable<SailDiffResult> TestCaseResults, out string ResultsAsMarkdown)
    {
        TestCaseResults = this.TestCaseResults;
        ResultsAsMarkdown = this.ResultsAsMarkdown;
    }
}