using MediatR;
using Sailfish.Analysis.ScaleFish;
using System.Collections.Generic;

namespace Sailfish.Contracts.Public.Notifications;

public record ScaleFishAnalysisCompleteNotification(string ScaleFishResultMarkdown, List<ScalefishClassModel> TestClassComplexityResults) : INotification;
