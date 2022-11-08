using System.Collections.Generic;
using Accord.Collections;
using Sailfish.Execution;

namespace Sailfish.Presentation.Console;

internal interface IConsoleWriter
{
    string Present(List<ExecutionSummary> result, OrderedDictionary<string, string> tags);
}