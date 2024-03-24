using System.Collections.Generic;
using MediatR;
using Sailfish.Analysis.ScaleFish;

namespace Sailfish.Contracts.Public.Notifications;

public record ScaleFishAnalysisCompleteNotification(string ScaleFishResultMarkdown, List<ScalefishClassModel> TestClassComplexityResults) : INotification;
