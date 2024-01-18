﻿using MediatR;
using Sailfish.Execution;
using System.Collections.Generic;

namespace Sailfish.Contracts.Private;

internal class WriteToMarkDownNotification(List<IClassExecutionSummary> classExecutionSummaries) : INotification
{
    public List<IClassExecutionSummary> ClassExecutionSummaries { get; } = classExecutionSummaries;
}