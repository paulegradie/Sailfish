﻿using MediatR;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using System.Collections.Generic;

namespace Sailfish.Contracts.Public.Notifications;

public record TestRunCompletedNotification(IEnumerable<ClassExecutionSummaryTrackingFormat> ClassExecutionSummaries) : INotification;