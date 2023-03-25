using System.Collections.Generic;
using System.Collections.Specialized;
using Sailfish.Execution;

namespace Sailfish.Presentation.Console;

internal interface IConsoleWriter
{
    string Present(IEnumerable<IExecutionSummary> result, OrderedDictionary tags);
}