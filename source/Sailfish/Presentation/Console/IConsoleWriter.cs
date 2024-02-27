using Sailfish.Execution;
using Sailfish.Extensions.Types;
using System.Collections.Generic;

namespace Sailfish.Presentation.Console;

public interface IConsoleWriter
{
    string WriteToConsole(IEnumerable<IClassExecutionSummary> result, OrderedDictionary tags);

    void WriteString(string content);
}