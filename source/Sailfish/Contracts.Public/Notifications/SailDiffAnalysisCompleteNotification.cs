using MediatR;
using Sailfish.Contracts.Public.Models;
using System.Collections.Generic;

namespace Sailfish.Contracts.Public.Notifications;

public record SailDiffAnalysisCompleteNotification(IEnumerable<SailDiffResult> TestCaseResults, string ResultsAsMarkdown) : INotification;