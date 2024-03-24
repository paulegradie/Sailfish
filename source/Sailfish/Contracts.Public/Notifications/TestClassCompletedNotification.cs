using System.Collections.Generic;
using MediatR;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;

namespace Sailfish.Contracts.Public.Notifications;

public record TestClassCompletedNotification(
    ClassExecutionSummaryTrackingFormat ClassExecutionSummaryTrackingFormat,
    TestInstanceContainerExternal TestInstanceContainerExternal,
    IEnumerable<dynamic> TestCaseGroup) : INotification;