using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Execution;

namespace Sailfish.Presentation;

internal interface ITestResultPresenter
{
    Task PresentResults(
        List<IExecutionSummary> resultContainers,
        DateTime timeStamp,
        string trackingDir,
        RunSettings runSettings,
        CancellationToken cancellationToken);
}