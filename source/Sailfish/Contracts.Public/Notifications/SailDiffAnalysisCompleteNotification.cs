using System.Collections.Generic;
using MediatR;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Contracts.Public.Notifications;

public class SailDiffAnalysisCompleteNotification : INotification
{
    public IEnumerable<SailDiffResult> TestCaseResults { get; }
    public string ResultsAsMarkdown { get; }

    public SailDiffAnalysisCompleteNotification(IEnumerable<SailDiffResult> testCaseResults, string resultsAsMarkdown)
    {
        TestCaseResults = testCaseResults;
        ResultsAsMarkdown = resultsAsMarkdown;
    }
}