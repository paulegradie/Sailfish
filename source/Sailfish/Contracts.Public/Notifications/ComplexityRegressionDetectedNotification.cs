using System.Collections.Generic;
using MediatR;
using Sailfish.Analysis.ScaleFish.Trends;

namespace Sailfish.Contracts.Public.Notifications;

/// <summary>
/// Published when ScaleFish's trend tracking detects one or more non-stable transitions between the
/// current run and the most-recent prior snapshot. Consumers (CI scripts, IDE plugins, custom handlers)
/// can subscribe to fail builds, post PR comments, or surface alerts.
/// </summary>
public record ComplexityRegressionDetectedNotification : INotification
{
    public ComplexityRegressionDetectedNotification(IReadOnlyList<ComplexityTransition> Regressions)
    {
        this.Regressions = Regressions;
    }

    public IReadOnlyList<ComplexityTransition> Regressions { get; init; }
}
