using System.Collections.Generic;
using Sailfish.Statistics;

namespace Sailfish.Presentation.Console;

internal interface IConsoleWriter
{
    string Present(List<ExecutionSummary> result);
}