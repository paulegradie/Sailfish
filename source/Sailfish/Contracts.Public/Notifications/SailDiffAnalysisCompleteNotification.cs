using MediatR;
using Sailfish.Contracts.Public.Models;
using System.Collections.Generic;

namespace Sailfish.Contracts.Public.Notifications;

public class SailDiffAnalysisCompleteNotification(IEnumerable<SailDiffResult> testCaseResults, string resultsAsMarkdown) : INotification
{
    public IEnumerable<SailDiffResult> TestCaseResults { get; } = testCaseResults;
    public string ResultsAsMarkdown { get; } = resultsAsMarkdown;
}