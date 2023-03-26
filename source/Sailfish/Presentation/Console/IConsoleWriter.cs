using System.Collections.Generic;
using Sailfish.Execution;
using Sailfish.Extensions.Types;

namespace Sailfish.Presentation.Console;

internal interface IConsoleWriter
{
    string Present(IEnumerable<IExecutionSummary> result, OrderedDictionary tags);
}