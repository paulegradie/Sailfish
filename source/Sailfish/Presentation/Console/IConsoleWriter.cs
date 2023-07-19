using System.Collections.Generic;
using Sailfish.Analysis;
using Sailfish.Execution;
using Sailfish.Extensions.Types;

namespace Sailfish.Presentation.Console;

public interface IConsoleWriter
{
    string Present(IEnumerable<IExecutionSummary> result, OrderedDictionary tags);
    void WriteStatTestResultsToConsole(string markdownBody, TestIds testIds, TestSettings testSettings);
    void WriteString(string content);
}