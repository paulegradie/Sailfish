using System.Collections.Generic;
using MediatR;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;

namespace Sailfish.Contracts.Public.Notifications;

public record TestCaseCompletedNotification : INotification
{
    public TestCaseCompletedNotification(ClassExecutionSummaryTrackingFormat ClassExecutionSummaryTrackingFormat,
        TestInstanceContainerExternal TestInstanceContainerExternal,
        IEnumerable<dynamic> TestCaseGroup)
    {
        this.ClassExecutionSummaryTrackingFormat = ClassExecutionSummaryTrackingFormat;
        this.TestInstanceContainerExternal = TestInstanceContainerExternal;
        this.TestCaseGroup = TestCaseGroup;
    }

    public ClassExecutionSummaryTrackingFormat ClassExecutionSummaryTrackingFormat { get; init; }
    public TestInstanceContainerExternal TestInstanceContainerExternal { get; init; }
    public IEnumerable<dynamic> TestCaseGroup { get; init; }

    public void Deconstruct(out ClassExecutionSummaryTrackingFormat ClassExecutionSummaryTrackingFormat, out TestInstanceContainerExternal TestInstanceContainerExternal, out IEnumerable<dynamic> TestCaseGroup)
    {
        ClassExecutionSummaryTrackingFormat = this.ClassExecutionSummaryTrackingFormat;
        TestInstanceContainerExternal = this.TestInstanceContainerExternal;
        TestCaseGroup = this.TestCaseGroup;
    }
}