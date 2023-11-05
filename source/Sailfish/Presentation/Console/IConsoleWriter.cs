using System.Collections.Generic;
using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff;
using Sailfish.Execution;
using Sailfish.Extensions.Types;

namespace Sailfish.Presentation.Console;

public interface IConsoleWriter
{
    string WriteToConsole(IEnumerable<IClassExecutionSummary> result, OrderedDictionary tags);
    void WriteStatTestResultsToConsole(string markdownBody, TestIds testIds, SailDiffSettings sailDiffSettings);
    void WriteString(string content);
}