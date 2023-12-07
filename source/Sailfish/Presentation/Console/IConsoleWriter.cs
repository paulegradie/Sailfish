using Sailfish.Analysis.SailDiff;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Extensions.Types;
using System.Collections.Generic;

namespace Sailfish.Presentation.Console;

public interface IConsoleWriter
{
    string WriteToConsole(IEnumerable<IClassExecutionSummary> result, OrderedDictionary tags);

    void WriteStatTestResultsToConsole(string markdownBody, TestIds testIds, SailDiffSettings sailDiffSettings);

    void WriteString(string content);
}