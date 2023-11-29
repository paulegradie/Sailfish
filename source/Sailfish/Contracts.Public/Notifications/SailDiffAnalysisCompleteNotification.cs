using System.Collections.Generic;
using MediatR;

namespace Sailfish.Contracts.Public.Notifications;

public class SailDiffAnalysisCompleteNotification : INotification
{
    public IEnumerable<TestCaseResults> TestCaseResults { get; }
    public string ResultsAsMarkdown { get; }

    public SailDiffAnalysisCompleteNotification(IEnumerable<TestCaseResults> testCaseResults, string resultsAsMarkdown)
    {
        TestCaseResults = testCaseResults;
        ResultsAsMarkdown = resultsAsMarkdown;
    }
}