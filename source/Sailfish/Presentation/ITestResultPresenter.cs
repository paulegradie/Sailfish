using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sailfish.Presentation.TTest;
using Sailfish.Statistics;

namespace Sailfish.Presentation;

public interface ITestResultPresenter
{
    Task PresentResults(
        List<CompiledResultContainer> resultContainers,
        string directoryPath,
        DateTime timeStamp,
        bool noTrack,
        bool analyze,
        TTestSettings settings);
}