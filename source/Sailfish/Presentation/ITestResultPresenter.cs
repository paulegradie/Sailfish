using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sailfish.Presentation.TTest;
using Sailfish.Statistics;

namespace Sailfish.Presentation;

internal interface ITestResultPresenter
{
    Task PresentResults(
        List<ExecutionSummary> resultContainers,
        string directoryPath,
        string trackingDirectory,
        DateTime timeStamp,
        bool noTrack,
        bool analyze,
        bool notify,
        TTestSettings settings);
}