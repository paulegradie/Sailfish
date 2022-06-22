using System.Collections.Generic;
using Sailfish.Statistics;

namespace Sailfish.Presentation.Console;

public interface IConsoleWriter
{
    string Present(List<CompiledResultContainer> result);
}