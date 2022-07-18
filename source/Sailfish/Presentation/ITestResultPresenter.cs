using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sailfish.Statistics;

namespace Sailfish.Presentation;

internal interface ITestResultPresenter
{
    Task PresentResults(
        List<ExecutionSummary> resultContainers,
        DateTime timeStamp,
        RunSettings runSettings);
}