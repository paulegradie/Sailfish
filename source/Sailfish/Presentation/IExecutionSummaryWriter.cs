using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Execution;

namespace Sailfish.Presentation;

internal interface IExecutionSummaryWriter
{
    Task Write(
        List<IClassExecutionSummary> executionSummaries,
        DateTime timeStamp,
        CancellationToken cancellationToken);
}