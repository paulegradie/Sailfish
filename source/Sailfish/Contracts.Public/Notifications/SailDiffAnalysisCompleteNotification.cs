using System.Collections.Generic;
using MediatR;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Contracts.Public.Notifications;

public record SailDiffAnalysisCompleteNotification(
    IEnumerable<SailDiffResult> TestCaseResults,
    string ResultsAsMarkdown)
    : INotification;