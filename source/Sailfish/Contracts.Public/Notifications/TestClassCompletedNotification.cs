using MediatR;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using System.Collections.Generic;

namespace Sailfish.Contracts.Public.Notifications;

public record TestClassCompletedNotification(
    ClassExecutionSummaryTrackingFormat ClassExecutionSummaryTrackingFormat,
    TestInstanceContainerExternal TestInstanceContainerExternal,
    IEnumerable<dynamic> TestCaseGroup) : INotification;