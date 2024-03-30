using System.Collections.Generic;
using Sailfish.Execution;
using Sailfish.Extensions.Types;

namespace Sailfish.Presentation.Console;

public interface IConsoleWriter
{
    string WriteToConsole(IEnumerable<IClassExecutionSummary> result, OrderedDictionary tags);

    void WriteString(string content);
}